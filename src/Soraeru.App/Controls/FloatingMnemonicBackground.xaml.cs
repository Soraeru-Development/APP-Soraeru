using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace Soraeru.Controls;

/// <summary>
/// Stitch L00 ambient layer: soft washes + multilingual word pairs that fly across
/// the screen from eight edge directions (matches L00_code.html Web Animations loop).
/// </summary>
public partial class FloatingMnemonicBackground : ContentView
{
    static readonly (string Original, string Translit)[] WordPairs =
    [
        ("नमस्ते", "納瑪斯戴"),
        ("¡Hola!", "歐拉"),
        ("مرحبًا", "瑪爾哈班"),
        ("Bonjour", "崩啾"),
        ("Здравствуйте", "茲德拉斯維帖"),
        ("Guten Tag", "咕騰・塔格"),
        ("こんにちは", "摳尼吉哇"),
        ("สวัสดี", "薩瓦迪卡"),
        ("Xin chào", "辛昭"),
        ("안녕하세요", "安妞哈塞唷"),
    ];

    public static readonly BindableProperty TargetOpacityProperty = BindableProperty.Create(
        nameof(TargetOpacity),
        typeof(double),
        typeof(FloatingMnemonicBackground),
        0.85d);

    public static readonly BindableProperty AutoStartProperty = BindableProperty.Create(
        nameof(AutoStart),
        typeof(bool),
        typeof(FloatingMnemonicBackground),
        true);

    public static readonly BindableProperty SpawnIntervalMsProperty = BindableProperty.Create(
        nameof(SpawnIntervalMs),
        typeof(int),
        typeof(FloatingMnemonicBackground),
        300);

    readonly object _gate = new();
    readonly Random _rng = new();
    CancellationTokenSource? _loopCts;
    bool _running;
    int _lastDirection = -1;
    int _animSeq;

    public FloatingMnemonicBackground()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, _) => { /* layout ready for spawn coords */ };
    }

    /// <summary>Peak opacity of each flying word mid-flight (HTML uses ~0.8).</summary>
    public double TargetOpacity
    {
        get => (double)GetValue(TargetOpacityProperty);
        set => SetValue(TargetOpacityProperty, value);
    }

    public bool AutoStart
    {
        get => (bool)GetValue(AutoStartProperty);
        set => SetValue(AutoStartProperty, value);
    }

    /// <summary>How often a new word pair is spawned (HTML: 300ms).</summary>
    public int SpawnIntervalMs
    {
        get => (int)GetValue(SpawnIntervalMsProperty);
        set => SetValue(SpawnIntervalMsProperty, value);
    }

    void OnLoaded(object? sender, EventArgs e)
    {
        if (AutoStart)
            _ = StartAsync();
    }

    void OnUnloaded(object? sender, EventArgs e) => Stop();

    public Task RestartAsync()
    {
        Stop();
        return StartAsync();
    }

    public Task StartAsync()
    {
        CancellationToken token;
        lock (_gate)
        {
            if (_running)
                return Task.CompletedTask;

            _running = true;
            _loopCts = new CancellationTokenSource();
            token = _loopCts.Token;
        }

        return RunSpawnLoopAsync(token);
    }

    public void Stop()
    {
        lock (_gate)
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;
            _running = false;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                FloatingLayer.Children.Clear();
            }
            catch (ObjectDisposedException)
            {
                // Host torn down.
            }
        });
    }

    async Task RunSpawnLoopAsync(CancellationToken token)
    {
        try
        {
            // Wait until AbsoluteLayout has a real size (HTML uses container offsetWidth/Height).
            for (var i = 0; i < 40 && !token.IsCancellationRequested; i++)
            {
                if (FloatingLayer.Width > 8 && FloatingLayer.Height > 8)
                    break;
                await Task.Delay(50, token);
            }

            if (token.IsCancellationRequested)
                return;

            // Initial burst: 8 words staggered 150ms (L00_code.html).
            for (var i = 0; i < 8; i++)
            {
                if (token.IsCancellationRequested)
                    return;
                SpawnFlyingWord();
                await Task.Delay(150, token);
            }

            var interval = Math.Max(120, SpawnIntervalMs);
            while (!token.IsCancellationRequested)
            {
                SpawnFlyingWord();
                await Task.Delay(interval, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when leaving the host page.
        }
    }

    void SpawnFlyingWord()
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(SpawnFlyingWord);
            return;
        }

        var w = FloatingLayer.Width;
        var h = FloatingLayer.Height;
        if (w < 8 || h < 8 || !IsLoaded)
            return;

        // Cap concurrent cards so Android AbsoluteLayout stays responsive.
        if (FloatingLayer.Children.Count >= 18)
            return;

        var pair = WordPairs[_rng.Next(WordPairs.Length)];
        int direction;
        do
        {
            direction = _rng.Next(8);
        } while (direction == _lastDirection);
        _lastDirection = direction;

        var (startX, startY, endX, endY) = PickPath(direction, w, h);
        var rotStart = (_rng.NextDouble() - 0.5) * 45;
        var rotEnd = (_rng.NextDouble() - 0.5) * 45;
        var durationMs = 3000 + _rng.Next(2500);
        var peakOpacity = Math.Clamp(TargetOpacity, 0.35, 1.0);

        var card = BuildWordCard(pair.Original, pair.Translit);
        card.Opacity = 0;
        card.Rotation = rotStart;
        AbsoluteLayout.SetLayoutFlags(card, AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutBounds(card, new Rect(startX, startY, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        FloatingLayer.Children.Add(card);

        var dx = endX - startX;
        var dy = endY - startY;
        var animName = $"fly-{Interlocked.Increment(ref _animSeq)}";

        var animation = new Animation();
        animation.Add(0, 1, new Animation(v =>
        {
            card.TranslationX = dx * v;
            card.TranslationY = dy * v;
            card.Rotation = rotStart + (rotEnd - rotStart) * v;
            // Opacity: 0 → peak @20% → hold → 0 @100% (HTML keyframes).
            if (v < 0.2)
                card.Opacity = peakOpacity * (v / 0.2);
            else if (v < 0.8)
                card.Opacity = peakOpacity;
            else
                card.Opacity = peakOpacity * (1.0 - (v - 0.8) / 0.2);
        }));

        try
        {
            animation.Commit(
                card,
                animName,
                rate: 16,
                length: (uint)durationMs,
                easing: Easing.SinInOut,
                finished: (_, _) =>
                {
                    try
                    {
                        FloatingLayer.Children.Remove(card);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Host torn down.
                    }
                });
        }
        catch (ObjectDisposedException)
        {
            // Host torn down mid-spawn.
        }
    }

    (double startX, double startY, double endX, double endY) PickPath(int direction, double w, double h)
    {
        double startX, startY, endX, endY;
        switch (direction)
        {
            case 0: // Top → Bottom
                startX = _rng.NextDouble() * w;
                startY = -50;
                endX = startX + (_rng.NextDouble() - 0.5) * 150;
                endY = h + 50;
                break;
            case 1: // Right → Left
                startX = w + 50;
                startY = _rng.NextDouble() * h;
                endX = -50;
                endY = startY + (_rng.NextDouble() - 0.5) * 150;
                break;
            case 2: // Bottom → Top
                startX = _rng.NextDouble() * w;
                startY = h + 50;
                endX = startX + (_rng.NextDouble() - 0.5) * 150;
                endY = -50;
                break;
            case 3: // Left → Right
                startX = -50;
                startY = _rng.NextDouble() * h;
                endX = w + 50;
                endY = startY + (_rng.NextDouble() - 0.5) * 150;
                break;
            case 4: // TL → BR
                startX = -50;
                startY = -50;
                endX = w + 50;
                endY = h + 50;
                break;
            case 5: // TR → BL
                startX = w + 50;
                startY = -50;
                endX = -50;
                endY = h + 50;
                break;
            case 6: // BL → TR
                startX = -50;
                startY = h + 50;
                endX = w + 50;
                endY = -50;
                break;
            default: // BR → TL
                startX = w + 50;
                startY = h + 50;
                endX = -50;
                endY = -50;
                break;
        }

        return (startX, startY, endX, endY);
    }

    static View BuildWordCard(string original, string translit)
    {
        var primary = ResolveColor("Primary", Color.FromArgb("#004d64"));
        var secondary = ResolveColor("Secondary", Color.FromArgb("#4d616c"));

        var originalLabel = new Label
        {
            Text = original,
            TextColor = primary,
            FontSize = 26,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.NoWrap,
        };

        var translitLabel = new Label
        {
            Text = translit,
            TextColor = secondary,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.NoWrap,
        };

        var pill = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#CCFFFFFF"),
            Padding = new Thickness(10, 2),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            HorizontalOptions = LayoutOptions.Center,
            Content = translitLabel,
            Shadow = new Shadow
            {
                Brush = Colors.Black,
                Offset = new Point(0, 2),
                Radius = 6,
                Opacity = 0.06f,
            },
        };

        return new VerticalStackLayout
        {
            Spacing = -8,
            InputTransparent = true,
            Children = { originalLabel, pill },
        };
    }

    static Color ResolveColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var obj) == true && obj is Color color)
            return color;
        return fallback;
    }
}
