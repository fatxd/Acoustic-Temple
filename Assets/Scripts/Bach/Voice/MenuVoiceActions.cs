internal sealed class MenuVoiceActions
{
    private static readonly string[] NumberCommands =
    {
        "uno", "dos", "tres", "cuatro", "cinco",
        "seis", "siete", "ocho", "nueve", "diez"
    };

    private readonly BachMenuController menu;

    public MenuVoiceActions(BachMenuController menu)
    {
        this.menu = menu;
    }

    public void Handle(string command)
    {
        switch (menu.GameManager.CurrentState)
        {
            case GameState.MainMenu:
                if (command == "empezar") menu.StartRound();
                else if (command == "volumen") menu.OpenVolume();
                else if (command == "salir") menu.QuitGame();
                break;

            case GameState.Playing:
                if (command == "pausa") menu.PauseRound();
                break;

            case GameState.Paused:
                if (command == "continuar" || command == "reanudar") menu.ResumeRound();
                else if (command == "volver") menu.ReturnToMainMenu();
                else if (command == "volumen") menu.OpenVolume();
                else if (command == "salir") menu.QuitGame();
                break;

            case GameState.VolumeMenu:
            case GameState.PausedVolumeMenu:
                if (command == "volver") menu.CloseVolume();
                else if (command == "salir") menu.QuitGame();
                else
                {
                    int numberIndex = System.Array.IndexOf(NumberCommands, command);
                    if (numberIndex >= 0) menu.SetVolume(numberIndex + 1);
                }
                break;

            case GameState.Victory:
            case GameState.GameOver:
                if (command == "volver") menu.ReturnToMainMenu();
                else if (command == "salir") menu.QuitGame();
                break;
        }
    }
}
