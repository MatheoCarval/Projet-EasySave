using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace EasySave.View.Console;

/// <summary>
/// ConsoleUI class - Manages the terminal user interface for EasySave backup application
/// </summary>
internal class ConsoleUI
{
    /// <summary>Maximum number of backup jobs allowed</summary>
    private const int MAX_JOBS = 5;

    /// <summary>Maximum number of sources per backup job</summary>
    private const int MAX_SOURCES = 5;

    /// <summary>List of all backup jobs with their properties: id, name, sources, destinations, backup type</summary>
    private List<(int id, string name, List<string> sources, List<string> destinations, string backupType)> _jobs = new();

    /// <summary>Counter for generating unique job IDs</summary>
    private int _nextJobId = 1;

    /// <summary>Main application window</summary>
    private Window? _mainWindow;

    /// <summary>Content frame for displaying different screens</summary>
    private FrameView? _contentFrame;

    // TODO: _backupManager: BackupManager
    // TODO: _localizationService: LocalizationService
    // TODO: _configManager: ConfigurationManager

    /// <summary>
    /// Initializes and runs the terminal UI application
    /// </summary>
    public void Start()
    {
        Application.Init();
        var top = Application.Top;

        _mainWindow = CreateMainWindow();
        top.Add(_mainWindow);

        Application.Run(top);
        Application.Shutdown();
    }

    private Window CreateMainWindow()
    {
        var window = new Window("EasySave")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var menuBar = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_Menu", new MenuItem[]
            {
                new MenuItem("_Quitter", "", () => Application.RequestStop())
            })
        });
        window.Add(menuBar);

        _contentFrame = new FrameView("Menu Principal")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var mainMenu = CreateMenuListView();
        _contentFrame!.Add(mainMenu);
        window.Add(_contentFrame);

        return window;
    }

    private ListView CreateMenuListView()
    {
        var items = new List<string>
        {
            "Créer une tâche",
            "Modifier une tâche",
            "Supprimer une tâche",
            "Exécuter une tâche",
            "Exécuter toutes les tâches",
            "Afficher les tâches",
            "Modifier les paramètres",
            "Quitter"
        };

        var listView = new ListView(items)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 4,
            Width = 30,
            Height = 8,
            AllowsMarking = false,
            CanFocus = true
        };

        listView.OpenSelectedItem += (e) =>
        {
            HandleMenuSelection(listView.SelectedItem);
        };

        return listView;
    }

    private void HandleMenuSelection(int selected)
    {
        // Handle main menu selection and route to appropriate function
        switch (selected)
        {
            case 0:
                CreateNewJob();
                break;
            case 1:
                ModifyJob();
                break;
            case 2:
                DeleteJob();
                break;
            case 3:
                ExecuteJob();
                break;
            case 4:
                ExecuteAllJobs();
                break;
            case 5:
                DisplayJobs();
                break;
            case 6:
                ChangeSettings();
                break;
            case 7:
                Application.RequestStop();
                break;
        }
    }

    /// <summary>
    /// Creates a new backup job through a multi-step wizard
    /// Steps: 1) Job Name, 2) Sources, 3) Destination, 4) Backup Type, 5) Validation
    /// </summary>
    private void CreateNewJob()
    {
        if (_jobs.Count >= MAX_JOBS)
        {
            MessageBox.ErrorQuery("Erreur", $"Limite de {MAX_JOBS} tâches atteinte.", "OK");
            return;
        }

        var taskName = "";
        var sources = new List<string>();
        var destination = "";
        var backupType = "";

        // Étape 1 : Nom
        CreateJobStepName((name) =>
        {
            taskName = name;
            // Étape 2 : Sources
            CreateJobStepSources((sourcesResult) =>
            {
                sources = sourcesResult;
                // Étape 3 : Destination
                CreateJobStepDestination((dest) =>
                {
                    destination = dest;
                    // Étape 4 : Type de sauvegarde
                    CreateJobStepBackupType((type) =>
                    {
                        backupType = type;
                        // Étape 5 : Validation
                        CreateJobStepValidation(taskName, sources, destination, backupType);
                    });
                });
            });
        });
    }

    /// <summary>
    /// Step 1: Prompts user to enter the job name
    /// </summary>
    private void CreateJobStepName(Action<string> onComplete)
    {
        _contentFrame!.Title = "Créer une tâche - Étape 1/3";
        _contentFrame!.RemoveAll();

        var label = new Label("Nom de la tâche:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var nameField = new TextField("")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var nextBtn = new Button("Suivant")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        nextBtn.Clicked += () =>
        {
            var taskName = nameField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(taskName))
            {
                MessageBox.ErrorQuery("Erreur", "Le nom de la tâche est requis.", "OK");
                return;
            }
            onComplete(taskName);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, nameField, nextBtn, cancelBtn);
    }

    /// <summary>
    /// Step 2: Allows user to add source directories (up to MAX_SOURCES)
    /// </summary>
    private void CreateJobStepSources(Action<List<string>> onComplete)
    {
        var sources = new List<string>();
        AddSourceForm(sources, onComplete);
    }

    /// <summary>
    /// Recursive form for adding or modifying multiple sources
    /// Displays "Source X/Y" format to show progress
    /// </summary>
    private void AddSourceForm(List<string> sources, Action<List<string>> onComplete)
    {
        _contentFrame!.Title = $"Créer une tâche - Étape 2/4 (Source {sources.Count + 1}/{MAX_SOURCES})";
        _contentFrame!.RemoveAll();

        var label = new Label($"Source {sources.Count + 1}/{MAX_SOURCES}:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var sourceField = new TextField("")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var addBtn = new Button("Ajouter")
        {
            X = Pos.Center() - 25,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var skipBtn = new Button("Suivant")
        {
            X = Pos.Center() - 5,
            Y = Pos.Center() + 2
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 15,
            Y = Pos.Center() + 2
        };

        addBtn.Clicked += () =>
        {
            var source = sourceField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(source))
            {
                MessageBox.ErrorQuery("Erreur", "La source ne peut pas être vide.", "OK");
                return;
            }

            sources.Add(source);

            if (sources.Count < MAX_SOURCES)
            {
                int result = MessageBox.Query(50, 7, "Ajouter une source", "Ajouter une autre source ?", "Oui", "Non");
                if (result == 0)
                {
                    AddSourceForm(sources, onComplete);
                }
                else
                {
                    onComplete(sources);
                }
            }
            else
            {
                MessageBox.Query(50, 7, "Limite atteinte", $"Limite de {MAX_SOURCES} sources atteinte.", "OK");
                onComplete(sources);
            }
        };

        skipBtn.Clicked += () =>
        {
            if (sources.Count == 0)
            {
                MessageBox.ErrorQuery("Erreur", "Au moins une source est requise.", "OK");
                return;
            }
            onComplete(sources);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, sourceField, addBtn, skipBtn, cancelBtn);
    }

    /// <summary>
    /// Step 3: Prompts user to enter the backup destination path
    /// </summary>
    private void CreateJobStepDestination(Action<string> onComplete)
    {
        _contentFrame!.Title = "Créer une tâche - Étape 3/4";
        _contentFrame!.RemoveAll();

        var label = new Label("Destination:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var destField = new TextField("")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var infoLabel = new Label("Format: C:\\path\\to\\folder\\")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center()
        };

        var nextBtn = new Button("Suivant")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        nextBtn.Clicked += () =>
        {
            var destination = destField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(destination))
            {
                MessageBox.ErrorQuery("Erreur", "La destination est requise.", "OK");
                return;
            }
            onComplete(destination);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, destField, infoLabel, nextBtn, cancelBtn);
    }

    /// <summary>
    /// Step 4: Allows user to select backup type (Full or Differential)
    /// </summary>
    private void CreateJobStepBackupType(Action<string> onComplete)
    {
        _contentFrame!.Title = "Créer une tâche - Étape 4/4";
        _contentFrame!.RemoveAll();

        var label = new Label("Type de sauvegarde:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 4
        };

        var backupTypes = new List<string> { "Complète", "Différentielle" };
        var listView = new ListView(backupTypes)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 2,
            Width = 30,
            Height = 3,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button("Sélectionner")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        selectBtn.Clicked += () =>
        {
            var selectedType = backupTypes[listView.SelectedItem];
            onComplete(selectedType);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Step 5: Shows a summary of the job and asks for confirmation before saving
    /// </summary>
    private void CreateJobStepValidation(string taskName, List<string> sources, string destination, string backupType)
    {
        _contentFrame!.Title = "Créer une tâche - Validation";
        _contentFrame!.RemoveAll();

        var summary = $"Récapitulatif de la création\n\n" +
                     $"Nom: {taskName}\n" +
                     $"Sources:\n";

        for (int i = 0; i < sources.Count; i++)
        {
            summary += $"  [{i + 1}/{sources.Count}] {sources[i]}\n";
        }

        summary += $"Destination: {destination}\n" +
                   $"Type: {backupType}";

        var result = MessageBox.Query(60, 18, "Validation", summary, "Valider", "Annuler");

        if (result == 0)
        {
            var destinations = new List<string> { destination };
            _jobs.Add((_nextJobId++, taskName, sources, destinations, backupType));
            MessageBox.Query(50, 7, "Succès", "Tâche créée.", "OK");
            // TODO: Appeler BackupManager.CreateJob(taskName, sources, destination, backupType)
            DisplayMainMenu();
        }
        else
        {
            DisplayMainMenu();
        }
    }

    /// <summary>
    /// Executes a single selected backup job
    /// </summary>
    private void ExecuteJob()
    {
        if (_jobs.Count == 0)
        {
            MessageBox.ErrorQuery("Erreur", "Aucune tâche disponible.", "OK");
            return;
        }

        var jobNames = new List<string>();
        foreach (var job in _jobs)
        {
            jobNames.Add(job.name);
        }

        _contentFrame!.Title = "Exécuter une tâche";
        _contentFrame!.RemoveAll();

        var label = new Label("Choisissez une tâche à exécuter:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var listView = new ListView(jobNames)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3,
            Width = 40,
            Height = Math.Min(10, jobNames.Count + 1),
            AllowsMarking = false,
            CanFocus = true
        };

        var executeBtn = new Button("Exécuter")
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        executeBtn.Clicked += () =>
        {
            var selectedJob = _jobs[listView.SelectedItem];
            MessageBox.Query(50, 7, "Exécution", $"Tâche exécutée: {selectedJob.name}\nDate: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", "OK");
            // TODO: Appeler BackupManager.ExecuteJob(selectedJob.id)
            DisplayMainMenu();
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, executeBtn, cancelBtn);
    }

    /// <summary>
    /// Executes all backup jobs in the system
    /// </summary>
    private void ExecuteAllJobs()
    {
        if (_jobs.Count == 0)
        {
            MessageBox.ErrorQuery("Erreur", "Aucune tâche disponible.", "OK");
            return;
        }

        MessageBox.Query(50, 7, "Exécution", $"Exécution de toutes les tâches...\nDate: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nToutes les tâches ont été exécutées.", "OK");
        // TODO: Appeler BackupManager.ExecuteAll()
        DisplayMainMenu();
    }

    /// <summary>
    /// Modifies an existing backup job
    /// Allows user to select which field to modify (name, sources, destination, or backup type)
    /// </summary>
    private void ModifyJob()
    {
        if (_jobs.Count == 0)
        {
            MessageBox.ErrorQuery("Erreur", "Aucune tâche disponible.", "OK");
            return;
        }

        var jobNames = new List<string>();
        foreach (var job in _jobs)
        {
            jobNames.Add(job.name);
        }

        _contentFrame!.Title = "Modifier une tâche";
        _contentFrame!.RemoveAll();

        var label = new Label("Choisissez une tâche à modifier:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var listView = new ListView(jobNames)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3,
            Width = 40,
            Height = Math.Min(10, jobNames.Count + 1),
            AllowsMarking = false,
            CanFocus = true
        };

        var modifyBtn = new Button("Modifier")
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        modifyBtn.Clicked += () =>
        {
            var selectedIndex = listView.SelectedItem;
            var selectedJob = _jobs[selectedIndex];
            ShowModifyOptions(selectedIndex, selectedJob);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, modifyBtn, cancelBtn);
    }

    /// <summary>
    /// Displays menu for selecting which job parameter to modify
    /// </summary>
    private void ShowModifyOptions(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = "Modifier une tâche - Choisir le paramètre";
        _contentFrame!.RemoveAll();

        var label = new Label("Que souhaitez-vous modifier?")
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var options = new List<string> { "Nom de la tâche", "Sources", "Destination", "Type de sauvegarde" };
        var listView = new ListView(options)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 1,
            Width = 30,
            Height = 5,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button("Sélectionner")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 5
        };

        selectBtn.Clicked += () =>
        {
            switch (listView.SelectedItem)
            {
                case 0:
                    ModifyJobName(jobIndex, job);
                    break;
                case 1:
                    ModifyJobSources(jobIndex, job);
                    break;
                case 2:
                    ModifyJobDestination(jobIndex, job);
                    break;
                case 3:
                    ModifyJobBackupType(jobIndex, job);
                    break;
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Modifies the job name with confirmation before saving
    /// </summary>
    private void ModifyJobName(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = "Modifier - Nom de la tâche";
        _contentFrame!.RemoveAll();

        var label = new Label("Nouveau nom:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var nameField = new TextField(job.name)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var confirmBtn = new Button("Confirmer")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        confirmBtn.Clicked += () =>
        {
            var newName = nameField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.ErrorQuery("Erreur", "Le nom ne peut pas être vide.", "OK");
                return;
            }

            if (newName != job.name)
            {
                ShowModifyConfirmation(jobIndex, job, "Nom", job.name, newName, () =>
                {
                    _jobs[jobIndex] = (job.id, newName, job.sources, job.destinations, job.backupType);
                    MessageBox.Query(50, 7, "Succès", "Tâche modifiée.", "OK");
                    AskContinueModifying(jobIndex);
                });
            }
            else
            {
                MessageBox.Query(50, 7, "Information", "Aucune modification effectuée.", "OK");
                AskContinueModifying(jobIndex);
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, nameField, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Displays options to modify existing sources or add new ones
    /// </summary>
    private void ModifyJobSources(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = "Modifier - Sources";
        _contentFrame!.RemoveAll();

        var label = new Label("Que voulez-vous faire?")
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var options = new List<string> { "Modifier une source existante", "Ajouter une nouvelle source" };
        var listView = new ListView(options)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 1,
            Width = 40,
            Height = 3,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button("Sélectionner")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 3,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 3
        };

        selectBtn.Clicked += () =>
        {
            if (listView.SelectedItem == 0)
            {
                ShowEditSourcesList(jobIndex, job);
            }
            else
            {
                ShowAddNewSourcesForm(jobIndex, job);
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Shows list of existing sources for the user to select one to edit
    /// </summary>
    private void ShowEditSourcesList(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = "Modifier - Choisir une source";
        _contentFrame!.RemoveAll();

        var label = new Label("Sélectionnez une source à modifier:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var listView = new ListView(job.sources)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3,
            Width = 40,
            Height = Math.Min(10, job.sources.Count + 1),
            AllowsMarking = false,
            CanFocus = true
        };

        var editBtn = new Button("Modifier")
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        editBtn.Clicked += () =>
        {
            var sourceIndex = listView.SelectedItem;
            ShowEditSourceForm(jobIndex, job, sourceIndex);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, editBtn, cancelBtn);
    }

    /// <summary>
    /// Allows editing a specific source at the given index
    /// </summary>
    private void ShowEditSourceForm(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job, int sourceIndex)
    {
        _contentFrame!.Title = $"Modifier - Source {sourceIndex + 1}/{job.sources.Count}";
        _contentFrame!.RemoveAll();

        var label = new Label("Nouvelle valeur:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var sourceField = new TextField(job.sources[sourceIndex])
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var confirmBtn = new Button("Confirmer")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        confirmBtn.Clicked += () =>
        {
            var newSource = sourceField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(newSource))
            {
                MessageBox.ErrorQuery("Erreur", "La source ne peut pas être vide.", "OK");
                return;
            }

            if (newSource != job.sources[sourceIndex])
            {
                ShowModifyConfirmation(jobIndex, job, $"Source {sourceIndex + 1}", job.sources[sourceIndex], newSource, () =>
                {
                    var updatedSources = new List<string>(job.sources);
                    updatedSources[sourceIndex] = newSource;
                    _jobs[jobIndex] = (job.id, job.name, updatedSources, job.destinations, job.backupType);
                    MessageBox.Query(50, 7, "Succès", "Source modifiée.", "OK");
                    AskContinueModifying(jobIndex);
                });
            }
            else
            {
                MessageBox.Query(50, 7, "Information", "Aucune modification effectuée.", "OK");
                AskContinueModifying(jobIndex);
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, sourceField, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Form for adding new sources to an existing job
    /// </summary>
    private void ShowAddNewSourcesForm(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        var newSources = new List<string>(job.sources);
        AddModifySourceForm(newSources, job, jobIndex, () =>
        {
            if (newSources.Count > job.sources.Count)
            {
                ShowModifyConfirmation(jobIndex, job, "Sources", string.Join(", ", job.sources), string.Join(", ", newSources), () =>
                {
                    _jobs[jobIndex] = (job.id, job.name, newSources, job.destinations, job.backupType);
                    MessageBox.Query(50, 7, "Succès", "Sources modifiées.", "OK");
                    AskContinueModifying(jobIndex);
                });
            }
            else
            {
                MessageBox.Query(50, 7, "Information", "Aucune nouvelle source ajoutée.", "OK");
                AskContinueModifying(jobIndex);
            }
        });
    }

    /// <summary>
    /// Recursive form for adding or modifying sources during job editing
    /// </summary>
    private void AddModifySourceForm(List<string> sources, (int id, string name, List<string> sources, List<string> destinations, string backupType) job, int jobIndex, Action onComplete)
    {
        _contentFrame!.Title = $"Modifier - Source {sources.Count + 1}/{MAX_SOURCES}";
        _contentFrame!.RemoveAll();

        var label = new Label($"Source {sources.Count + 1}/{MAX_SOURCES}:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var sourceField = new TextField(sources.Count < job.sources.Count ? sources[sources.Count] : "")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var addBtn = new Button("Ajouter")
        {
            X = Pos.Center() - 25,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var skipBtn = new Button("Valider")
        {
            X = Pos.Center() - 5,
            Y = Pos.Center() + 2
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 15,
            Y = Pos.Center() + 2
        };

        addBtn.Clicked += () =>
        {
            var source = sourceField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(source))
            {
                MessageBox.ErrorQuery("Erreur", "La source ne peut pas être vide.", "OK");
                return;
            }

            if (sources.Count < job.sources.Count)
            {
                sources[sources.Count] = source;
            }
            else
            {
                sources.Add(source);
            }

            if (sources.Count < MAX_SOURCES)
            {
                int result = MessageBox.Query(50, 7, "Ajouter une source", "Ajouter une autre source ?", "Oui", "Non");
                if (result == 0)
                {
                    AddModifySourceForm(sources, job, jobIndex, onComplete);
                }
                else
                {
                    onComplete();
                }
            }
            else
            {
                MessageBox.Query(50, 7, "Limite atteinte", $"Limite de {MAX_SOURCES} sources atteinte.", "OK");
                onComplete();
            }
        };

        skipBtn.Clicked += () =>
        {
            if (sources.Count == 0)
            {
                MessageBox.ErrorQuery("Erreur", "Au moins une source est requise.", "OK");
                return;
            }
            onComplete();
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, sourceField, addBtn, skipBtn, cancelBtn);
    }

    /// <summary>
    /// Modifies the job destination with confirmation before saving
    /// </summary>
    private void ModifyJobDestination(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = "Modifier - Destination";
        _contentFrame!.RemoveAll();

        var label = new Label("Nouvelle destination:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var destField = new TextField(job.destinations[0])
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var infoLabel = new Label("Format: C:\\path\\to\\folder\\")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center()
        };

        var confirmBtn = new Button("Confirmer")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        confirmBtn.Clicked += () =>
        {
            var newDestination = destField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(newDestination))
            {
                MessageBox.ErrorQuery("Erreur", "La destination ne peut pas être vide.", "OK");
                return;
            }

            if (newDestination != job.destinations[0])
            {
                ShowModifyConfirmation(jobIndex, job, "Destination", job.destinations[0], newDestination, () =>
                {
                    var newDestinations = new List<string> { newDestination };
                    _jobs[jobIndex] = (job.id, job.name, job.sources, newDestinations, job.backupType);
                    MessageBox.Query(50, 7, "Succès", "Tâche modifiée.", "OK");
                    AskContinueModifying(jobIndex);
                });
            }
            else
            {
                MessageBox.Query(50, 7, "Information", "Aucune modification effectuée.", "OK");
                AskContinueModifying(jobIndex);
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, destField, infoLabel, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Modifies the backup type (Full or Differential) with confirmation before saving
    /// </summary>
    private void ModifyJobBackupType(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = "Modifier - Type de sauvegarde";
        _contentFrame!.RemoveAll();

        var label = new Label("Nouveau type de sauvegarde:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 4
        };

        var backupTypes = new List<string> { "Complète", "Différentielle" };
        var selectedIndex = backupTypes.IndexOf(job.backupType);
        if (selectedIndex < 0) selectedIndex = 0;

        var listView = new ListView(backupTypes)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 2,
            Width = 30,
            Height = 3,
            AllowsMarking = false,
            CanFocus = true,
            SelectedItem = selectedIndex
        };

        var confirmBtn = new Button("Confirmer")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        confirmBtn.Clicked += () =>
        {
            var newType = backupTypes[listView.SelectedItem];

            if (newType != job.backupType)
            {
                ShowModifyConfirmation(jobIndex, job, "Type", job.backupType, newType, () =>
                {
                    _jobs[jobIndex] = (job.id, job.name, job.sources, job.destinations, newType);
                    MessageBox.Query(50, 7, "Succès", "Tâche modifiée.", "OK");
                    AskContinueModifying(jobIndex);
                });
            }
            else
            {
                MessageBox.Query(50, 7, "Information", "Aucune modification effectuée.", "OK");
                AskContinueModifying(jobIndex);
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Prompts user to continue modifying other fields or return to main menu
    /// </summary>
    private void AskContinueModifying(int jobIndex)
    {
        var updatedJob = _jobs[jobIndex];
        var result = MessageBox.Query(50, 7, "Continuer", "Voulez-vous modifier un autre champ?", "Oui", "Non");

        if (result == 0)
        {
            ShowModifyOptions(jobIndex, updatedJob);
        }
        else
        {
            DisplayMainMenu();
        }
    }

    /// <summary>
    /// Shows a confirmation dialog with old and new values before applying changes
    /// </summary>
    private void ShowModifyConfirmation(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job, string parameterName, string oldValue, string newValue, Action onConfirm)
    {
        var summary = $"Confirmation de modification\n\n" +
                     $"Tâche: {job.name}\n" +
                     $"Paramètre: {parameterName}\n\n" +
                     $"Ancienne valeur:\n{oldValue}\n\n" +
                     $"Nouvelle valeur:\n{newValue}\n\n" +
                     $"Êtes-vous sûr de vouloir appliquer cette modification?";

        var result = MessageBox.Query(70, 20, "Confirmation", summary, "Oui, sauvegarder", "Non, annuler");

        if (result == 0)
        {
            onConfirm();
        }
        else
        {
            DisplayMainMenu();
        }
    }

    /// <summary>
    /// Deletes a selected backup job from the system
    /// </summary>
    private void DeleteJob()
    {
        if (_jobs.Count == 0)
        {
            MessageBox.ErrorQuery("Erreur", "Aucune tâche disponible.", "OK");
            return;
        }

        var jobNames = new List<string>();
        foreach (var job in _jobs)
        {
            jobNames.Add(job.name);
        }

        _contentFrame!.Title = "Supprimer une tâche";
        _contentFrame!.RemoveAll();

        var label = new Label("Choisissez une tâche à supprimer:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var listView = new ListView(jobNames)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3,
            Width = 40,
            Height = Math.Min(10, jobNames.Count + 1),
            AllowsMarking = false,
            CanFocus = true
        };

        var deleteBtn = new Button("Supprimer")
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        deleteBtn.Clicked += () =>
        {
            var selectedIndex = listView.SelectedItem;
            var selectedJob = _jobs[selectedIndex];

            // Ask for confirmation before deleting
            var result = MessageBox.Query(60, 10, "Confirmation de suppression",
                $"Êtes-vous sûr de vouloir supprimer la tâche:\n\n\"{selectedJob.name}\"?\n\nCette action est irréversible.",
                "Oui, supprimer", "Non, annuler");

            if (result == 0)
            {
                _jobs.RemoveAt(selectedIndex);
                MessageBox.Query(50, 7, "Succès", "Tâche supprimée.", "OK");
                // TODO: Appeler BackupManager.DeleteJob(selectedJob.id)
                DisplayMainMenu();
            }
            else
            {
                DisplayMainMenu();
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, deleteBtn, cancelBtn);
    }

    /// <summary>
    /// Displays detailed information about all backup jobs
    /// </summary>
    private void DisplayJobs()
    {
        if (_jobs.Count == 0)
        {
            MessageBox.ErrorQuery("Erreur", "Aucune tâche disponible.", "OK");
            return;
        }

        var jobNames = new List<string>();
        foreach (var job in _jobs)
        {
            jobNames.Add(job.name);
        }

        _contentFrame!.Title = "Afficher les tâches";
        _contentFrame!.RemoveAll();

        var label = new Label("Choisissez une tâche pour la détailler:")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var listView = new ListView(jobNames)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3,
            Width = 40,
            Height = Math.Min(10, jobNames.Count + 1),
            AllowsMarking = false,
            CanFocus = true
        };

        var detailBtn = new Button("Détails")
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button("Retour")
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        detailBtn.Clicked += () =>
        {
            var selectedJob = _jobs[listView.SelectedItem];
            var details = $"[{selectedJob.id}] {selectedJob.name}\n\n";
            details += "Sources:\n";
            for (int i = 0; i < selectedJob.sources.Count; i++)
            {
                details += $"  [{i + 1}/{selectedJob.sources.Count}] {selectedJob.sources[i]}\n";
            }
            details += $"\nDestination:\n";
            for (int i = 0; i < selectedJob.destinations.Count; i++)
            {
                details += $"  [{i + 1}] {selectedJob.destinations[i]}\n";
            }
            details += $"\nType de sauvegarde: {selectedJob.backupType}";
            MessageBox.Query(60, 18, "Détails", details, "OK");
            DisplayJobs();
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, detailBtn, cancelBtn);
    }

    /// <summary>
    /// Displays application settings menu for configuring language and log format
    /// </summary>
    private void ChangeSettings()
    {
        _contentFrame!.Title = "Modifier les paramètres";
        _contentFrame!.RemoveAll();

        var label = new Label("Choisissez un paramètre:")
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var options = new List<string> { "Choisir la langue", "Choisir le format de log" };
        var listView = new ListView(options)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 1,
            Width = 30,
            Height = 4,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button("Sélectionner")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        var cancelBtn = new Button("Retour")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

        selectBtn.Clicked += () =>
        {
            switch (listView.SelectedItem)
            {
                case 0:
                    ChooseLanguage();
                    break;
                case 1:
                    ChooseLogFormat();
                    break;
                case 2:
                    DisplayMainMenu();
                    break;
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Allows user to select the application language
    /// </summary>
    private void ChooseLanguage()
    {
        _contentFrame!.Title = "Choisir la langue";
        _contentFrame!.RemoveAll();

        var label = new Label("Sélectionnez la langue:")
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var languages = new List<string> { "Français", "English" };
        var listView = new ListView(languages)
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() - 1,
            Width = 20,
            Height = 4,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button("Sélectionner")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        var cancelBtn = new Button("Retour")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

        selectBtn.Clicked += () =>
        {
            // TODO: Appeler LocalizationService.ChangeLanguage(listView.SelectedItem)
            MessageBox.Query(50, 7, "Succès", "Langue modifiée.", "OK");
            ChangeSettings();
        };

        cancelBtn.Clicked += () =>
        {
            ChangeSettings();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Allows user to select the log file format (JSON or XML)
    /// </summary>
    private void ChooseLogFormat()
    {
        _contentFrame!.Title = "Choisir le format de log";
        _contentFrame!.RemoveAll();

        var label = new Label("Sélectionnez le format:")
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var formats = new List<string> { "JSON", "XML" };
        var listView = new ListView(formats)
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() - 1,
            Width = 20,
            Height = 3,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button("Sélectionner")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        var cancelBtn = new Button("Retour")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

        selectBtn.Clicked += () =>
        {
            // TODO: Appeler ConfigurationManager.UpdateLogFormat(listView.SelectedItem)
            MessageBox.Query(50, 7, "Succès", "Format de log modifié.", "OK");
            ChangeSettings();
        };

        cancelBtn.Clicked += () =>
        {
            ChangeSettings();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Returns to the main menu screen
    /// </summary>
    private void DisplayMainMenu()
    {
        _contentFrame!.Title = "Menu Principal";
        _contentFrame!.RemoveAll();
        _contentFrame!.Add(CreateMenuListView());
    }
}
