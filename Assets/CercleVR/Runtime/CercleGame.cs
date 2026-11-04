using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CercleVR.Core;
using UnityEngine;
using PaperLabel = UnityEngine.UI.Text;

namespace CercleVR
{
    public sealed class CercleGame : MonoBehaviour
    {
        [Range(7, 12)] public int npcCount = 7;
        [Range(0, 2)] public int lesson = 0;
        [Range(1, 2)] public int evilCount = 1;
        public int seed = 20261022;
        [Range(1, 50)] public int campaignLevels = 6;
        [Range(1, 10)] public float nextLevelDelay = 3f;
        public Transform clipboardAnchor;
        public Vector3 clipboardViewOffset = new Vector3(-0.34f, -0.10f, 1.0f);
        private Session session;
        private CampaignProgress campaign;
        private Coroutine nextLevelRoutine;
        private int puzzleRevision;
        private Transform actors, doorPivot, dialogue, clipboard;
        private Transform[] characterRoots;
        private PaperLabel journal, header, documentTitle, documentPage, deskNotice;
        private PaperLabel dialogueTitle, dialogueRole, dialogueStatement, dialogueInfo;
        private GameObject markAction, accuseAction, confirmAction, cancelAction;
        private GameObject retryAction, readingAction;
        private GameObject[] seatPlates;
        private PaperLabel[] seatLabels, tokenLabels;
        private Renderer[] clipboardTabs = new Renderer[5];
        private Renderer[] bodies;
        private Color[] colors;
        private Font font;
        private int selected = -1, pending = -1;
        private bool generating;
        private int panelMode, panelPage;
        private string notice = "Carnet : C / bouton X gauche pour le rappeler.";
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Material> actorMaterials = new List<Material>();
        private bool actorPhase;
        private static readonly string[] Names = { "Aline", "Basile", "Celia", "Dorian", "Elise", "Farid", "Gael", "Helene", "Idris", "Jade", "Karim", "Lina" };
        private static readonly string[] Marks = { "", "? Suspect", "A verifier", "Fiable ?" };
        private static readonly Color Ink = new Color(0.15f, 0.11f, 0.075f);
        private static readonly Color Paper = new Color(0.95f, 0.89f, 0.72f);
        private static readonly Color Accent = new Color(0.40f, 0.20f, 0.09f);

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            campaign = new CampaignProgress(campaignLevels);
            BuildRoom();
            BeginLevel();
        }
        private Material Material(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader); material.color = color;
            (actorPhase ? actorMaterials : materials).Add(material);
            return material;
        }
        private GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color, Action action = null)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (action != null) go.AddComponent<WorldAction>().Bind(action);
            return go;
        }
        private GameObject ActionButton(string caption, Transform parent, Vector3 position, float width, float height, Action action, Color color,
            int fontSize = PaperLayout.ButtonSize, float depth = 0.025f)
        {
            var root = new GameObject("Action " + caption); root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            Shape(caption, PrimitiveType.Cube, root.transform, Vector3.zero, new Vector3(width, height, depth), color, action);
            var canvas = PaperLayout.CreateCanvas(root.transform, "Texte du bouton", width, height, -depth / 2 - 0.002f);
            float padX = Math.Min(0.03f, width * 0.06f), padY = Math.Min(0.025f, height * 0.10f);
            PaperLayout.Label(canvas, font, caption + " texte", -width / 2 + padX, height / 2 - padY,
                width - padX * 2, height - padY * 2, fontSize, Ink, caption, TextAnchor.MiddleCenter);
            return root;
        }
        private void BuildClipboard()
        {
            clipboard = new GameObject("Clipboard d'enquete").transform;
            clipboard.SetParent(transform, false);
            Shape("Planche clipboard", PrimitiveType.Cube, clipboard, Vector3.zero, new Vector3(0.56f, 0.82f, 0.018f), new Color(0.30f, 0.20f, 0.11f));
            Shape("Feuille clipboard", PrimitiveType.Cube, clipboard, new Vector3(0, 0, -0.012f), new Vector3(0.53f, 0.77f, 0.003f), Paper);
            Shape("Pince metallique", PrimitiveType.Cube, clipboard, new Vector3(0, 0.377f, -0.022f), new Vector3(0.16f, 0.045f, 0.014f), new Color(0.60f, 0.63f, 0.65f));
            var canvas = PaperLayout.CreateCanvas(clipboard, "Mise en page clipboard", 0.53f, 0.77f, -0.016f);
            var title = PaperLayout.Label(canvas, font, "Titre carnet", -0.24f, 0.34f, 0.48f, 0.038f, 26, Accent, "CARNET D'ENQUETE");
            title.fontStyle = FontStyle.Bold;
            header = PaperLayout.Label(canvas, font, "Etat partie", -0.24f, 0.294f, 0.48f, 0.060f, 20, Ink);
            var tabColor = new Color(0.87f, 0.80f, 0.64f);
            string[] tabs = { "Journal", "Aide", "Oracle", "Roles", "Dossier" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int mode = i;
                var button = ActionButton(tabs[i], clipboard, new Vector3((i - 2) * 0.102f, 0.198f, -0.020f),
                    0.096f, 0.05f, () => ShowPanel(mode), tabColor, 18, 0.012f);
                clipboardTabs[i] = button.GetComponentInChildren<Renderer>();
            }
            documentTitle = PaperLayout.Label(canvas, font, "Titre document", -0.24f, 0.153f, 0.48f, 0.034f, 22, Accent, "JOURNAL D'ENQUETE");
            documentTitle.fontStyle = FontStyle.Bold;
            journal = PaperLayout.Label(canvas, font, "Journal", -0.24f, 0.100f, 0.48f, 0.320f, 22, Ink);
            documentPage = PaperLayout.Label(canvas, font, "Pagination papier", -0.24f, -0.242f, 0.48f, 0.026f, 18, Accent);
            deskNotice = PaperLayout.Label(canvas, font, "Note enqueteur", -0.24f, -0.280f, 0.48f, 0.055f, 18, Ink);
            var ruleColor = new Color(0.50f, 0.39f, 0.23f, 0.65f);
            PaperLayout.Rule(canvas, -0.24f, 0.112f, 0.48f, ruleColor);
            PaperLayout.Rule(canvas, -0.24f, -0.231f, 0.48f, ruleColor);
            ActionButton("Page -", clipboard, new Vector3(-0.182f, -0.370f, -0.020f), 0.116f, 0.05f,
                () => { panelPage = Math.Max(0, panelPage - 1); Refresh(); }, tabColor, 18, 0.012f);
            ActionButton("Page +", clipboard, new Vector3(-0.060f, -0.370f, -0.020f), 0.116f, 0.05f,
                () => { panelPage++; Refresh(); }, tabColor, 18, 0.012f);
            readingAction = ActionButton("Lire", clipboard, new Vector3(0.062f, -0.370f, -0.020f), 0.116f, 0.05f,
                ToggleClipboardReading, tabColor, 18, 0.012f);
            retryAction = ActionButton("Reessayer", clipboard, new Vector3(0.184f, -0.370f, -0.020f), 0.116f, 0.05f,
                RetryLevel, tabColor, 16, 0.012f);
            retryAction.SetActive(false);
            var mount = clipboard.gameObject.AddComponent<ClipboardMount>();
            mount.anchorOverride = clipboardAnchor;
            mount.viewOffset = clipboardViewOffset;
            mount.ReadingChanged += reading =>
                readingAction.GetComponentInChildren<PaperLabel>().text = reading ? "Reduire" : "Lire";
            // LateUpdate places the board after the tracked camera's first pose update.
        }
        private void ToggleClipboardReading()
        {
            var mount = clipboard.GetComponent<ClipboardMount>();
            mount.ToggleReading();
            readingAction.GetComponentInChildren<PaperLabel>().text = mount.Reading ? "Reduire" : "Lire";
        }
        private void BuildDialogue()
        {
            dialogue = new GameObject("Dialogue du personnage selectionne").transform;
            dialogue.SetParent(transform, false);
            dialogue.gameObject.AddComponent<FaceCamera>().keepUpright = false;
            Shape("Bordure dialogue", PrimitiveType.Cube, dialogue, new Vector3(0, 0, 0.015f), new Vector3(1.84f, 1.06f, 0.025f), Accent);
            Shape("Fond dialogue", PrimitiveType.Cube, dialogue, Vector3.zero, new Vector3(1.80f, 1.02f, 0.035f), Paper);
            var canvas = PaperLayout.CreateCanvas(dialogue, "Texte fixe du dialogue", 1.80f, 1.02f, -0.0195f);
            dialogueTitle = PaperLayout.Label(canvas, font, "Nom personnage", -0.79f, 0.44f, 1.58f, 0.10f, 52, Accent);
            dialogueTitle.fontStyle = FontStyle.Bold; dialogueTitle.color = Accent;
            dialogueRole = PaperLayout.Label(canvas, font, "Role annonce", -0.79f, 0.29f, 1.58f, 0.075f, 40, Ink);
            dialogueStatement = PaperLayout.Label(canvas, font, "Declaration", -0.79f, 0.16f, 1.58f, 0.22f, 44, Ink);
            dialogueInfo = PaperLayout.Label(canvas, font, "Informations personnage", -0.79f, -0.12f, 1.58f, 0.16f, 40, Ink);
            var neutral = new Color(0.80f, 0.78f, 0.63f);
            var red = new Color(0.88f, 0.58f, 0.47f);
            markAction = ActionButton("Marquer", dialogue, new Vector3(-0.58f, -0.405f, -0.043f), 0.52f, 0.15f, Mark, neutral);
            accuseAction = ActionButton("Accuser", dialogue, new Vector3(0, -0.405f, -0.043f), 0.52f, 0.15f, Accuse, red);
            confirmAction = ActionButton("Confirmer", dialogue, new Vector3(-0.58f, -0.405f, -0.043f), 0.52f, 0.15f, Confirm, red);
            cancelAction = ActionButton("Annuler", dialogue, new Vector3(0, -0.405f, -0.043f), 0.52f, 0.15f, Cancel, neutral);
            ActionButton("Fermer", dialogue, new Vector3(0.58f, -0.405f, -0.043f), 0.52f, 0.15f, Deselect, neutral);
            dialogue.gameObject.SetActive(false);
        }
        private void BuildRoom()
        {
            Color stone = new Color(0.16f, 0.19f, 0.22f), wood = new Color(0.25f, 0.15f, 0.08f);
            // A separate floor/teleport surface is created by the Editor menu.
            Shape("Mur gauche", PrimitiveType.Cube, transform, new Vector3(-6, 1.5f, 0), new Vector3(0.2f, 3, 12), stone);
            Shape("Mur droit", PrimitiveType.Cube, transform, new Vector3(6, 1.5f, 0), new Vector3(0.2f, 3, 12), stone);
            Shape("Mur entree", PrimitiveType.Cube, transform, new Vector3(0, 1.5f, -6), new Vector3(12, 3, 0.2f), stone);
            Shape("Fond gauche", PrimitiveType.Cube, transform, new Vector3(-3.6f, 1.5f, 6), new Vector3(4.8f, 3, 0.2f), stone);
            Shape("Fond droit", PrimitiveType.Cube, transform, new Vector3(3.6f, 1.5f, 6), new Vector3(4.8f, 3, 0.2f), stone);
            Shape("Table", PrimitiveType.Cylinder, transform, new Vector3(0, 0.77f, 0), new Vector3(2.3f, 0.07f, 2.3f), wood);
            Shape("Pied table", PrimitiveType.Cylinder, transform, new Vector3(0, 0.35f, 0), new Vector3(0.35f, 0.35f, 0.35f), wood);
            doorPivot = new GameObject("Porte pivot").transform; doorPivot.SetParent(transform, false); doorPivot.localPosition = new Vector3(-1.1f, 0, 6);
            Shape("Porte", PrimitiveType.Cube, doorPivot, new Vector3(1.1f, 1.3f, 0), new Vector3(2.2f, 2.6f, 0.15f), wood);
            BuildClipboard();
            BuildDialogue();
        }
        private void BeginLevel()
        {
            puzzleRevision++;
            if (nextLevelRoutine != null) { StopCoroutine(nextLevelRoutine); nextLevelRoutine = null; }
            npcCount = campaign.NpcCount;
            lesson = campaign.Lesson;
            evilCount = campaign.EvilCount;
            StartCoroutine(NewPuzzle());
        }
        private void RetryLevel()
        {
            if (generating || campaign.WaitingForNextLevel) return;
            if (session != null && session.Outcome == Outcome.Playing) return;
            if (campaign.Completed) { campaign.Restart(); seed = unchecked(seed + 1000003); }
            else if (session == null) seed = unchecked(seed + 7919);
            BeginLevel();
        }
        private IEnumerator AdvanceAfterDelay(int revision)
        {
            yield return new WaitForSecondsRealtime(Math.Max(1f, nextLevelDelay));
            if (revision != puzzleRevision || session == null || session.Outcome != Outcome.Won) yield break;
            nextLevelRoutine = null;
            if (campaign.Advance()) BeginLevel();
        }
        private IEnumerator NewPuzzle()
        {
            generating = true; selected = pending = -1; session = null; panelMode = panelPage = 0;
            retryAction.SetActive(false);
            dialogue.gameObject.SetActive(false);
            documentTitle.text = "JOURNAL D'ENQUETE"; documentPage.text = ""; deskNotice.text = "";
            header.text = "NIVEAU " + campaign.LevelNumber + " / " + campaign.TotalLevels + "\nPreparation de l'enigme...";
            journal.text = "Verification des solutions possibles.";
            if (actors != null) Destroy(actors.gameObject);
            foreach (var material in actorMaterials) Destroy(material); actorMaterials.Clear();
            doorPivot.localRotation = Quaternion.identity;
            Puzzle puzzle = null;
            for (int attempt = 0; attempt < 256; attempt++)
            {
                puzzle = PuzzleEngine.TryGenerate(unchecked(campaign.SeedForLevel(seed) + attempt * 7919), npcCount, lesson, evilCount);
                if (puzzle != null) break;
                yield return null;
            }
            if (puzzle == null)
            {
                generating = false; header.text = "Generation interrompue";
                journal.text = "Aucune enigme valide trouvee. Appuyer sur Reessayer.\nAucune enigme ambigue n'est utilisee.";
                retryAction.SetActive(true); yield break;
            }
            session = new Session(puzzle); BuildActors();
            generating = false; notice = "Pointer un personnage ou son jeton pour l'interroger.";
            Refresh();
        }
        private void BuildActors()
        {
            actorPhase = true;
            actors = new GameObject("Personnages et jetons").transform; actors.SetParent(transform, false);
            characterRoots = new Transform[npcCount];
            seatLabels = new PaperLabel[npcCount]; tokenLabels = new PaperLabel[npcCount]; seatPlates = new GameObject[npcCount];
            bodies = new Renderer[npcCount]; colors = new Color[npcCount];
            for (int i = 0; i < npcCount; i++)
            {
                int id = i; float angle = i * Mathf.PI * 2 / npcCount;
                Vector3 radial = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                colors[i] = Color.HSVToRGB(i / (float)npcCount, 0.5f, 0.75f);
                var actor = new GameObject("PNJ " + (i + 1) + " " + Names[i]).transform;
                characterRoots[i] = actor;
                actor.SetParent(actors, false); actor.localPosition = radial * 2.8f;
                actor.localRotation = Quaternion.LookRotation(-radial);
                Color chair = new Color(0.26f, 0.18f, 0.10f);
                Shape("Assise", PrimitiveType.Cube, actor, new Vector3(0, 0.65f, 0), new Vector3(0.65f, 0.12f, 0.65f), chair);
                Shape("Dossier", PrimitiveType.Cube, actor, new Vector3(0, 1.0f, -0.3f), new Vector3(0.65f, 0.65f, 0.10f), chair);
                bodies[i] = Shape("Corps", PrimitiveType.Capsule, actor, new Vector3(0, 1.12f, 0), new Vector3(0.4f, 0.38f, 0.3f), colors[i], () => Select(id)).GetComponent<Renderer>();
                Shape("Tete", PrimitiveType.Sphere, actor, new Vector3(0, 1.66f, 0), Vector3.one * 0.33f, new Color(0.72f, 0.53f, 0.38f), () => Select(id));
                for (int side = -1; side <= 1; side += 2)
                {
                    Shape("Cuisse", PrimitiveType.Cube, actor, new Vector3(side * 0.14f, 0.75f, 0.25f), new Vector3(0.16f, 0.17f, 0.5f), colors[i]);
                    Shape("Jambe", PrimitiveType.Cube, actor, new Vector3(side * 0.14f, 0.38f, 0.43f), new Vector3(0.14f, 0.6f, 0.14f), colors[i]);
                    Shape("Oeil", PrimitiveType.Sphere, actor, new Vector3(side * 0.065f, 1.7f, 0.145f), Vector3.one * 0.04f, Color.black);
                }
                seatPlates[i] = new GameObject("Plaque de nom " + (i + 1));
                var namePlate = seatPlates[i].transform;
                namePlate.SetParent(actors, false); namePlate.localPosition = radial * 2.8f + Vector3.up * 2.0f;
                namePlate.gameObject.AddComponent<FaceCamera>();
                Shape("Support nom", PrimitiveType.Cube, namePlate, Vector3.zero, new Vector3(0.80f, 0.18f, 0.012f), Paper, () => Select(id));
                var nameCanvas = PaperLayout.CreateCanvas(namePlate, "Nom imprime", 0.80f, 0.18f, -0.009f);
                seatLabels[i] = PaperLayout.Label(nameCanvas, font, "Nom et repere", -0.38f, 0.075f, 0.76f, 0.15f, 48, Ink,
                    "", TextAnchor.MiddleCenter);
                var tokenPosition = radial * 0.83f + Vector3.up * 0.88f;
                Shape("Jeton " + (i + 1), PrimitiveType.Cylinder, actors, tokenPosition, new Vector3(0.28f, 0.035f, 0.28f), colors[i], () => Select(id));
                var tokenInk = new GameObject("Impression jeton " + (i + 1)).transform;
                tokenInk.SetParent(actors, false); tokenInk.localPosition = tokenPosition + Vector3.up * 0.0365f;
                tokenInk.localRotation = Quaternion.Euler(90, 0, 0);
                var tokenCanvas = PaperLayout.CreateCanvas(tokenInk, "Numero du jeton", 0.20f, 0.20f, 0);
                tokenLabels[i] = PaperLayout.Label(tokenCanvas, font, "Numero grave sur jeton", -0.10f, 0.10f, 0.20f, 0.20f, 64, Ink,
                    "", TextAnchor.MiddleCenter);
            }
            actorPhase = false;
        }
        private void Select(int seat)
        {
            if (generating || session == null || seat < 0 || seat >= session.Puzzle.Count) return;
            selected = seat; pending = -1;
            session.Reveal(seat);
            notice = "Dialogue ouvert : #" + (seat + 1) + " " + Names[seat] + "."; Refresh();
        }
        private void Deselect()
        {
            selected = pending = -1;
            if (panelMode == 4) { panelMode = 0; panelPage = 0; }
            notice = "Selectionner un personnage ou son jeton pour discuter."; Refresh();
        }
        private void Cancel()
        {
            pending = -1; notice = "Accusation annulee."; Refresh();
        }
        private void ShowPanel(int mode)
        {
            panelMode = panelMode == mode ? 0 : mode; panelPage = 0; Refresh();
        }
        private void Mark()
        {
            if (session == null || selected < 0 || session.Eliminated[selected] || session.Outcome != Outcome.Playing) return;
            pending = -1; session.Marks[selected] = (session.Marks[selected] + 1) % Marks.Length; Refresh();
        }
        private void Accuse()
        {
            if (session == null || selected < 0 || session.Eliminated[selected] || session.Outcome != Outcome.Playing) return;
            pending = selected; notice = "Confirmer l'elimination de " + Names[pending] + " (#" + (pending + 1) + ") ?"; Refresh();
        }
        private void Confirm()
        {
            if (session == null || pending < 0 || pending != selected) return;
            int target = pending; pending = -1;
            if (!session.Execute(target)) return;
            notice = Names[target] + (PuzzleEngine.Has(session.Puzzle.EvilMask, target) ? " etait malefique." : " etait innocent(e). -5 points de sante.");
            if (session.Outcome == Outcome.Won && campaign.RecordVictory())
            {
                if (campaign.Completed)
                {
                    doorPivot.localRotation = Quaternion.Euler(0, -100, 0);
                    notice = "Tous les niveaux sont reussis. La porte est ouverte.";
                }
                else
                {
                    notice = "Niveau reussi. Passage au niveau " + (campaign.LevelNumber + 1) + "...";
                    nextLevelRoutine = StartCoroutine(AdvanceAfterDelay(puzzleRevision));
                }
            }
            Refresh();
        }
        private void Refresh()
        {
            if (session == null) return;
            var p = session.Puzzle;
            header.text = "NIVEAU " + campaign.LevelNumber + "/" + campaign.TotalLevels + "   |   SANTE " + session.Health + "/10"
                + "\nPNJ " + p.Count + " | Malefiques " + session.EvilsKilled + "/" + p.EvilCount + " | Corrompus " + p.Tier;
            deskNotice.text = campaign.Completed ? "Campagne terminee. La porte est ouverte."
                : campaign.WaitingForNextLevel ? "Niveau reussi. Passage au niveau " + (campaign.LevelNumber + 1) + "..."
                : session.Outcome == Outcome.Lost ? "Echec. Reessayer ce niveau avec 10 points de sante." : notice;
            retryAction.SetActive(session.Outcome == Outcome.Lost || campaign.Completed);
            retryAction.GetComponentInChildren<PaperLabel>().text = campaign.Completed ? "Recommencer" : "Reessayer";
            for (int tab = 0; tab < clipboardTabs.Length; tab++)
                clipboardTabs[tab].sharedMaterial.color = panelMode == tab ? new Color(0.98f, 0.83f, 0.47f) : new Color(0.87f, 0.80f, 0.64f);
            var text = new StringBuilder();
            string title = "JOURNAL D'ENQUETE";
            if (panelMode == 1)
            {
                title = "REGLES ET COMMANDES";
                text.AppendLine("Eliminer TOUS les malefiques valide le niveau.");
                text.AppendLine("Le niveau suivant commence apres un court bilan.");
                text.AppendLine("La porte s'ouvre uniquement apres le dernier niveau.");
                text.AppendLine("Les innocents sains disent vrai. Les autres disent faux.");
                text.AppendLine("Les nombres de malefiques et de corrompus sont connus.");
                if (p.Tier > 0) text.AppendLine("Un empoisonneur malefique corrompt UN voisin innocent.");
                if (p.Tier > 1) text.AppendLine("Un AUTRE innocent est ivre. Il donne aussi des infos fausses.");
                text.AppendLine("Les indices decrivent le debut de cette enigme.");
                text.AppendLine("Un siege retire reste compte dans les distances.");
                text.AppendLine("Plusieurs personnages peuvent annoncer le meme role.");
                text.AppendLine("Un marqueur est VOTRE hypothese, jamais une preuve.");
                text.AppendLine("Selectionner un personnage ou son jeton ouvre son dialogue.");
                text.AppendLine("Marquer et Accuser sont dans ce dialogue.");
                text.AppendLine("Accuser affiche Confirmer et Annuler. Fermer annule aussi.");
                text.AppendLine("La difficulte augmente au fil des niveaux.");
                text.AppendLine("Oracle : historique public. Roles : familles d'indices.");
                text.AppendLine("Les onglets du clipboard changent le document.");
                text.AppendLine("Page - et Page + parcourent les notes.");
                text.AppendLine("C ou bouton X gauche : rappeler le carnet devant soi.");
                text.AppendLine("En VR, le carnet reste dans le monde pendant la lecture.");
                text.AppendLine("Innocent execute : -5 sante. A zero, l'affaire est perdue.");
                text.AppendLine("Sieges fixes : 1 > 2 > 3... dans le sens horaire vu du dessus.");
            }
            else if (panelMode == 2)
            {
                title = "ORACLE - HISTORIQUE PUBLIC";
                for (int i = 0; i < session.History.Count; i++)
                {
                    var item = session.History[i];
                    text.Append(item.Sequence).Append(". #").Append(item.Seat + 1).Append(" ").AppendLine(item.Text);
                    text.AppendLine();
                }
                if (session.History.Count == 0) text.AppendLine("Aucun temoignage recueilli.");
                text.AppendLine("Les anciens temoignages sont conserves apres une execution.");
            }
            else if (panelMode == 3)
            {
                title = "CATALOGUE DES ROLES";
                text.AppendLine("Guetteur : nombre de voisins malefiques (0, 1 ou 2).");
                text.AppendLine("Pisteur : distance au plus proche autre malefique.");
                text.AppendLine("Boussole : sens horaire, antihoraire ou egalite des distances.");
                text.AppendLine("Temoin : declare le camp d'une cible attribuee par l'enigme.");
                text.AppendLine("Plusieurs personnages peuvent annoncer le meme role.");
                text.AppendLine("Les ennemis utilisent les memes annonces que les innocents.");
                text.AppendLine("L'Oracle conserve les annonces. Le Dossier montre le suspect.");
            }
            else if (panelMode == 4)
            {
                title = "DOSSIER DU PERSONNAGE";
                if (selected < 0) text.AppendLine("Selectionnez un personnage ou son jeton pour consulter son dossier.");
                else
                {
                    int i = selected;
                    title = "DOSSIER #" + (i + 1) + " - " + Names[i];
                    text.AppendLine("Identite publique : " + session.PublicIdentity(i));
                    text.AppendLine("Marque personnelle : " + (session.Marks[i] == 0 ? "aucune" : Marks[session.Marks[i]]));
                    text.AppendLine("Ordre d'interrogatoire : " + session.RevealOrder[i]);
                    if (session.Revealed[i] || session.Outcome != Outcome.Playing)
                    {
                        text.AppendLine("Role annonce : " + PuzzleEngine.RoleName(p.Clues[i].Kind));
                        text.AppendLine("Declaration initiale : " + PuzzleEngine.Statement(p, i));
                    }
                    if (session.Eliminated[i]) text.AppendLine("Personnage elimine. Son siege reste compte.");
                }
            }
            else
            {
                for (int i = 0; i < p.Count; i++)
                {
                    text.Append(selected == i ? "> " : "").Append(i + 1).Append(" ").AppendLine(Names[i]);
                    if (session.Revealed[i]) text.AppendLine(PuzzleEngine.RoleName(p.Clues[i].Kind) + " : " + PuzzleEngine.Statement(p, i));
                    else text.AppendLine("Non interroge(e).");
                    if (session.Marks[i] != 0) text.AppendLine("Note : " + Marks[session.Marks[i]]);
                    if (session.Eliminated[i]) text.AppendLine("Elimine(e) : " + session.PublicIdentity(i));
                    text.AppendLine();
                }
            }
            if (session.Outcome != Outcome.Playing)
            {
                text.AppendLine();
                text.AppendLine(session.Outcome == Outcome.Won
                    ? (campaign.Completed ? "CAMPAGNE TERMINEE : la porte est ouverte." : "NIVEAU REUSSI : prochaine enquete en preparation.")
                    : "ECHEC : sante epuisee. Reessayer ce niveau.");
                text.Append("Revelation : malefiques ");
                for (int i = 0; i < p.Count; i++) if (PuzzleEngine.Has(p.EvilMask, i)) text.Append(i + 1).Append(" ");
                if (p.Poisoned >= 0) text.Append("| empoisonneur ").Append(p.Poisoner + 1).Append(" > ").Append(p.Poisoned + 1);
                if (p.Drunk >= 0) text.Append(" | ivre ").Append(p.Drunk + 1);
            }
            DisplayDocument(title, text.ToString());
            for (int i = 0; i < p.Count; i++)
            {
                string state = session.Eliminated[i] ? "Elimine(e)" : Marks[session.Marks[i]];
                seatLabels[i].text = (i + 1) + " - " + Names[i] + (state.Length == 0 ? "" : "\n" + state);
                seatPlates[i].SetActive(selected != i);
                tokenLabels[i].text = (i + 1) + "\n" + (selected == i ? ">" : "") + (session.Eliminated[i] ? "X" : session.Marks[i] == 0 ? "" : "?");
                bodies[i].sharedMaterial.color = session.Eliminated[i] ? Color.gray : (selected == i ? Color.white : colors[i]);
            }
            RefreshDialogue();
        }
        private void RefreshDialogue()
        {
            bool visible = !generating && session != null && selected >= 0 && selected < session.Puzzle.Count;
            dialogue.gameObject.SetActive(visible);
            if (!visible) return;
            int i = selected;
            var p = session.Puzzle;
            // Above the head, in world space. Only the selected character gets a bubble.
            dialogue.position = characterRoots[i].position + Vector3.up * 2.53f;
            dialogue.GetComponent<FaceCamera>().FaceNow();
            dialogueTitle.text = "#" + (i + 1) + "  " + Names[i];
            dialogueRole.text = "Role annonce : " + PuzzleEngine.RoleName(p.Clues[i].Kind);
            dialogueStatement.text = "\"" + PuzzleEngine.Statement(p, i) + "\"";
            bool canAct = session.Outcome == Outcome.Playing && !session.Eliminated[i];
            bool confirming = canAct && pending == i;
            if (confirming)
                dialogueInfo.text = "Eliminer #" + (i + 1) + " " + Names[i] + " ?\nUn innocent coute 5 points de sante.";
            else if (session.Eliminated[i] || session.Outcome != Outcome.Playing)
                dialogueInfo.text = "Identite revelee : " + session.PublicIdentity(i) + "\n" + (session.Eliminated[i] ? "Personnage elimine." : "Affaire terminee.");
            else
                dialogueInfo.text = "Marque : " + (session.Marks[i] == 0 ? "aucune" : Marks[session.Marks[i]])
                    + "\nInterrogatoire #" + session.RevealOrder[i] + " | Declaration conservee";
            markAction.SetActive(canAct && !confirming);
            accuseAction.SetActive(canAct && !confirming);
            confirmAction.SetActive(confirming);
            cancelAction.SetActive(confirming);
        }
        private void DisplayDocument(string title, string content)
        {
            // The font stays fixed; real font metrics determine how much fits on a sheet.
            var pages = PaperLayout.Pages(journal, content);
            panelPage = Math.Max(0, panelPage) % pages.Count;
            documentTitle.text = title;
            journal.text = pages[panelPage];
            documentPage.text = "PAGE " + (panelPage + 1) + " / " + pages.Count;
        }
        private void OnDestroy()
        {
            puzzleRevision++;
            if (nextLevelRoutine != null) { StopCoroutine(nextLevelRoutine); nextLevelRoutine = null; }
            // The clipboard is parented to the player, so destroy it explicitly with its owner.
            if (clipboard != null) Destroy(clipboard.gameObject);
            foreach (var m in materials) if (m != null) Destroy(m);
            foreach (var m in actorMaterials) if (m != null) Destroy(m);
        }
#if UNITY_EDITOR
        [ContextMenu("Tests/Valider le niveau en cours")]
        private void ValidateLevelForEditor()
        {
            if (!Application.isPlaying || generating || session == null || session.Outcome != Outcome.Playing) return;
            for (int i = 0; i < session.Puzzle.Count; i++)
                if (!session.Eliminated[i] && PuzzleEngine.Has(session.Puzzle.EvilMask, i))
                { Select(i); Accuse(); Confirm(); }
        }
#endif
    }
}
