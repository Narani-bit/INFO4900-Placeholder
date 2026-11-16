# CercleVR — PC et Meta Quest

Projet Unity **6000.6.0f1**. Les scènes existantes sont :

- `Assets/CercleVR/Scenes/CerclePC.unity` : clavier/souris.
- `Assets/CercleVR/Scenes/CercleQuest.unity` : XR Interaction Toolkit / OpenXR.

Identité validée le 1 novembre 2026 : société et produit **CercleVR**,
identifiant d'application **`com.cerclevr.game`**, version **`0.1.0`**.
Ces valeurs sont également définies dans les overrides du profil Meta Quest.

## Ouvrir le projet

Installer Git LFS avant le clonage, puis exécuter `git lfs pull` dans le dépôt.
Ajouter le dossier dans Unity Hub et ouvrir avec la version indiquée dans
`ProjectSettings/ProjectVersion.txt`. Attendre la résolution des paquets et
l'importation des ressources. Les Starter Assets XR et leurs `.meta` sont inclus :
ne pas les réimporter avec de nouveaux GUID.

Ouvrir l'une des scènes puis lancer Play. Sur PC : ZQSD ou WASD pour se déplacer,
bouton droit maintenu pour regarder, clic gauche pour interagir.

Le carnet se rappelle avec **C**, ou **X sur la manette gauche** sur Quest. En VR,
il suit la main gauche hors lecture ; **Lire** le fixe dans le monde à un mètre
et **Réduire** le ramène à la main. La scène PC conserve le suivi de la vue.
La scène Quest utilise une origine au sol, la téléportation, la rotation par crans
de 45° et une vignette de confort. Les destinations occupées par le mobilier sont refusées.

Les menus **Cercle VR / Creer scene** ouvrent la scène existante sans la recréer,
ou créent la scène canonique si elle manque. Ils réutilisent `Assets/CercleVR/Materials/Sol.mat`
et assignent `Base.mat` au jeu pour inclure le shader URP dans les builds.
Il n'est pas nécessaire de régénérer les scènes pour jouer.

## Compiler

Dans **File > Build Profiles**, sélectionner puis activer le profil souhaité :

| Profil | Cible | Scène incluse |
| --- | --- | --- |
| Cercle PC | Windows Player, 64 bits | CerclePC |
| Meta Quest | Android, IL2CPP ARM64 | CercleQuest |

Chaque profil remplace la liste globale par sa propre scène. Vérifier la cible et
la liste de scènes après activation, puis compiler dans `Builds/`.

- **Windows IL2CPP** : installer le module Windows Build Support (IL2CPP),
  Visual Studio avec les outils C++ et un SDK Windows compatible (au minimum
  10.0.19041 pour la vérification effectuée). Le backend IL2CPP est conservé.
- **Quest** : installer Android Build Support avec SDK/NDK et OpenJDK via Unity Hub.
  Utiliser le profil Meta Quest existant et vérifier OpenXR Project Validation.
  L'installation sur un casque nécessite le mode développeur et un appareil connecté.

Le connecteur d'audit `com.unity.pipeline` est facultatif et n'est pas imposé au
projet. Avec sa version 0.9.1-exp.1, passer explicitement la cible et la scène aux
builds automatisés : sélectionner uniquement un nom de profil peut laisser les
anciennes valeurs globales dans la commande. Même avec cible/scène explicites,
les overrides graphiques ne sont pas garantis par cette commande du connecteur.
Préférer **Cercle VR > Build > Quest - Developpement**, **Quest - Livraison**
(ou **PC avec son profil**),
qui transmet le profil à l'API native Unity `BuildPlayerWithProfileOptions`.
En automatisation Unity : `-executeMethod CercleVR.Editor.CercleProfileBuild.QuestDevelopment`,
`.QuestRelease` ou `.PC`, une fois le profil actif et son importation terminée.
Les sorties Quest sont `Builds/Quest-Development/CercleQuest.apk` et
`Builds/Quest-Release/CercleQuest.apk`. Le rapport voisin contient options, erreurs,
avertissements et SHA-256. La commande de livraison désactive explicitement
Development, Script Debugging, attente du débogueur et profilage connecté.
Elle aligne aussi la plateforme sélectionnée sur la cible active pour la validation
OpenXR et refuse un rapport contenant une erreur, même si Unity indique `Succeeded`.
La permission Internet est retirée de la version hors ligne par le traitement du
manifeste Android ; revoir cette règle si une fonction réseau est ajoutée au jeu.

Le niveau de qualité Meta Quest et le pipeline de secours global référencent
`Assets/Settings/Quest_RPAsset.asset`, copie locale du pipeline URP Quest initial.
Ses réglages restent **MSAA désactivé (1)** et **Render Scale 1**. Le pipeline
Mobile existant conserve ses propres réglages ; aucun de ces assets n'est ajusté
pour modifier la résolution ou l'anticrénelage dans cette correction.


Les APK/AAB, symboles natifs, clés de signature et fichiers de secrets sont exclus
de Git. Livrer l'APK validé via une **Release GitHub** ou le lien convenu avec
l'enseignante, avec la version et le commit correspondants. Conserver les symboles
avec le build et les clés de signature dans un stockage privé. Aucune clé ou secret
ne doit être joint à une issue. La publication du livrable final reste à effectuer
après la validation sur casque.

## Signaler et suivre les bogues

Utiliser le modèle **Bug** dans **New issue**, puis renseigner le commit, la version,
le mode de test, la scène, le niveau et la graine. Le modèle sera proposé par GitHub
après fusion de son ajout sur la branche principale.

## Contributions et accès au dépôt

Le dépôt reste **privé jusqu'à la remise**. Ne jamais pousser directement sur `main`,
réécrire son historique ou la supprimer. Chaque changement passe par une branche
courte, une pull request et la revue d'un autre membre avant fusion.

Le propriétaire vérifie les collaborateurs (équipe et enseignante si demandé).
Chaque membre active lui-même la double authentification de son compte GitHub.
Ces réglages personnels et l'approbation humaine ne sont pas remplacés par les tests.

## Tests de logique

Dans Unity : **Window > General > Test Runner > EditMode > Run All**.
L'assembly `CercleVR.Core.Tests` contient **761 tests** : 720 cas de génération
(7 à 12 PNJ, 3 leçons, 1 à 2 maléfiques, 20 graines), 36 cas de déterminisme,
la campagne et les règles de santé/victoire. `CercleVR.Core` n'a aucune référence Unity.

Les scripts de contrôle en mode Play se trouvent dans `Tests/Unity/`.
`IssuePlayChecks.Main` parcourt les six niveaux, contrôle les matériaux, dialogues,
le carnet et, dans la scène Quest, la téléportation et la vignette avec des poses
et entrées synthétiques. Exécuter via `script run` du connecteur Pipeline après
avoir lancé Play ; recommencer Play entre deux campagnes. Ce contrôle ne remplace
pas la lecture et le confort dans le casque.

Avec le SDK .NET 10 installé, depuis la racine du dépôt :

```sh
dotnet run --project Tests/CoreChecks/CoreChecks.csproj --configuration Release
```

Ce harnais compile directement les sources Core du projet et vérifie 732
assertions sur 24 énigmes et 4 campagnes : solution unique, cohérence des indices,
révélations, exécutions, santé, victoire/défaite, progression et redémarrage.
Il échoue avec un code de sortie non nul si une assertion échoue.
Il ne remplace pas les essais Unity, des entrées réelles et du casque.

Dans Unity, le menu **Cercle VR > Tests > Verifier les principes des cours**
contrôle les réglages XR, les repères et le placement de lecture hors Play.
