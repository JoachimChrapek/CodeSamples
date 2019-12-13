using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MenuContext
{
    Main,
    Load,
    Save,
    Settings,
    NewGame
}

public enum MenuUiElement
{
    MainPanel,
    LoadPanel,
    SavePanel,
    SettingsPanel,
    NewGamePanel
}

public class MenuUiController : UiController<MenuUiElement, MenuContext>
{
    public static MenuUiController instance = null;

    public void NewGame()
    {
        SwitchContext(MenuContext.Main);
        ScenesCommunicator.instance.OnNewGame();
    }

    public override void PreviousContext()
    {
        if (currentContext == MenuContext.Load || currentContext == MenuContext.Settings || currentContext == MenuContext.NewGame)
        {
            SwitchContext(MenuContext.Main);
        }
    }

    public override void SwitchContext(MenuContext newContext)
    {
        switch (newContext)
        {
            case MenuContext.Main:
                SetElementsState(MenuUiElement.MainPanel);
                break;
            case MenuContext.NewGame:
                SetElementsState(MenuUiElement.NewGamePanel);
                break;
            case MenuContext.Load:
                SetElementsState(MenuUiElement.LoadPanel);
                break;
            case MenuContext.Settings:
                SetElementsState(MenuUiElement.SettingsPanel);
                break;
            case MenuContext.Save:
                SetElementsState(MenuUiElement.SavePanel);
                break;
        }

        currentContext = newContext;
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        FindAllElements();
        
        SwitchContext(MenuContext.Main);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PreviousContext();
        }
    }
}

