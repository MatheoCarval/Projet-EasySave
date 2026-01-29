using Terminal.Gui;

namespace EasySave;

/// <summary>
/// Point d'entrée principal de l'application EasySave
/// </summary>
internal class Program
{
    private static Window? _mainWindow;

    /// <summary>
    /// Point d'entrée de l'application
    /// </summary>
    private static void Main(string[] args)
    {
        try
        {
            InitializeApplication();
            Application.Run();
        }
        finally
        {
            Application.Shutdown();
        }
    }

    /// <summary>
    /// Initialise l'interface utilisateur de l'application
    /// </summary>
    private static void InitializeApplication()
    {
        Application.Init();

        var top = Application.Top;

        // Créer et ajouter la barre de menu
        var menuBar = CreateMenuBar(top);
        top.Add(menuBar);

        // Créer et ajouter la fenêtre principale
        _mainWindow = CreateMainWindow();
        top.Add(_mainWindow);
    }

    /// <summary> 
    /// Crée la barre de menu principale
    /// </summary>
    private static MenuBar CreateMenuBar(Toplevel top)
    {
        return new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_Fichier", new MenuItem[]
            {
                new MenuItem("_Nouveau", string.Empty, OnNewProject),
                new MenuItem("_Ouvrir", string.Empty, OnOpenProject),
                new MenuItem("_Quitter", string.Empty, () => OnQuit(top))
            }),
            new MenuBarItem("_Aide", new MenuItem[]
            {
                new MenuItem("_À propos", string.Empty, OnAbout)
            })
        });
    }

    /// <summary>
    /// Crée la fenêtre principale avec ses contrôles
    /// </summary>
    private static Window CreateMainWindow()
    {
        var window = new Window("Bienvenue dans EasySave")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Ajouter le label d'instruction
        var label = new Label("Sélectionnez une option :")
        {
            X = Pos.Center(),
            Y = 2
        };
        window.Add(label);

        // Créer les boutons d'action
        var btnStart = new Button("Démarrer la sauvegarde")
        {
            X = Pos.Center(),
            Y = 5
        };
        btnStart.Clicked += OnStartBackup;

        var btnConfig = new Button("Configuration")
        {
            X = Pos.Center(),
            Y = 7
        };
        btnConfig.Clicked += OnOpenConfiguration;

        var btnExit = new Button("Quitter")
        {
            X = Pos.Center(),
            Y = 9
        };
        btnExit.Clicked += OnExitButtonClicked;

        window.Add(btnStart, btnConfig, btnExit);

        return window;
    }

    #region Event Handlers

    private static void OnNewProject()
    {
        MessageBox.Query("Nouveau", "Créer un nouveau projet", "OK");
    }

    private static void OnOpenProject()
    {
        MessageBox.Query("Ouvrir", "Ouvrir un projet", "OK");
    }

    private static void OnAbout()
    {
        MessageBox.Query("À propos", "EasySave - Version 1.0", "OK");
    }

    private static void OnStartBackup()
    {
        MessageBox.Query("Action", "Sauvegarde démarrée!", "OK");
    }

    private static void OnOpenConfiguration()
    {
        MessageBox.Query("Configuration", "Ouvrir la configuration", "OK");
    }

    private static void OnExitButtonClicked()
    {
        if (ConfirmQuit())
        {
            Application.RequestStop();
        }
    }

    private static void OnQuit(Toplevel top)
    {
        if (ConfirmQuit())
        {
            top.Running = false;
        }
    }

    #endregion

    /// <summary>
    /// Affiche une boîte de dialogue de confirmation pour quitter l'application
    /// </summary>
    /// <returns>True si l'utilisateur confirme, sinon False</returns>
    private static bool ConfirmQuit()
    {
        int result = MessageBox.Query(50, 7, "Quitter", "Êtes-vous sûr de vouloir quitter?", "Oui", "Non");
        return result == 0;
    }
}