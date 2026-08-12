using Soraeru.Services.Interfaces;



namespace Soraeru.Pages;



public partial class OnboardingPage : ContentPage

{

    private readonly ISoraeruApiClient _api;

    private readonly IAuthSessionStore _session;

    private bool _busy;



    public OnboardingPage(ISoraeruApiClient api, IAuthSessionStore session)

    {

        InitializeComponent();

        _api = api;

        _session = session;

    }



    async void OnStartClicked(object? sender, EventArgs e)

    {

        if (_busy)

            return;



        _busy = true;

        StartButton.IsEnabled = false;

        try

        {

            var me = await _api.PatchMeAsync(onboardingCompleted: true);

            var token = await _session.GetAccessTokenAsync();

            if (me is null || string.IsNullOrWhiteSpace(token))

            {

                await DisplayAlertAsync(

                    "無法完成設定",

                    "無法標記首次說明為已完成，請確認網路與登入狀態後再試。",

                    "確定");

                return;

            }



            await _session.SetSessionAsync(token, me.UserId, me.Email, me.OnboardingCompleted);

            await Routes.GoToMainTabAsync(Routes.Home);

        }

        catch (Exception ex)

        {

            await DisplayAlertAsync(

                "無法完成設定",

                $"無法連線 API，請稍後再試。\n{ex.Message}",

                "確定");

        }

        finally

        {

            _busy = false;

            StartButton.IsEnabled = true;

        }

    }

}


