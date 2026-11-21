# CercleVR sur Meta Quest 3

CercleVR est un jeu d’enquête en réalité virtuelle. Ce guide explique comment récupérer le projet sur **Windows**, le compiler avec Unity et installer le fichier Android obtenu, appelé **APK**, sur un **Meta Quest 3**.

Le Quest 3 est déjà configuré et son mode développeur est actif. Si vous disposez déjà de `CercleQuest.apk`, passez à [l’ajout de votre profil](#4-ajouter-un-profil-sur-le-quest-3) ou, si votre nom figure déjà dans le casque, directement à [l’installation du jeu](#5-installer-et-tester-le-jeu).

## 1. Télécharger le projet GitHub

1. Installez **Git pour Windows**. Ouvrez un nouveau terminal **PowerShell** et exécutez `git --version` puis `git lfs version`. Si la seconde commande est inconnue, installez **Git LFS** et rouvrez PowerShell. Git LFS télécharge les ressources volumineuses du projet. [Git pour Windows](https://git-scm.com/install/windows) · [Git LFS](https://git-lfs.com/)
2. Dans l’Explorateur Windows, ouvrez le dossier où vous voulez placer le projet. Cliquez dans la barre d’adresse, tapez `powershell` et appuyez sur **Entrée**. Exécutez les commandes suivantes.

   ```powershell
   git lfs install
   git clone https://github.com/Narani-bit/INFO4900-Placeholder.git
   cd .\INFO4900-Placeholder
   git lfs pull
   ```

3. Ouvrez le dossier `INFO4900-Placeholder`. Les dossiers **Assets**, **Packages** et **ProjectSettings** doivent se trouver au même niveau. C’est ce dossier que vous allez ouvrir dans Unity Hub. Si le projet est déjà cloné, exécutez simplement `git lfs pull` depuis ce dossier.

## 2. Installer Unity Hub, Unity et les extensions

1. Téléchargez **Unity Hub** pour Windows, lancez l’installateur, puis ouvrez le Hub et connectez-vous avec votre compte Unity. Si le Hub demande une licence, activez la licence correspondant à votre compte, par exemple **Unity Personal** pour un projet étudiant admissible. [Télécharger Unity Hub](https://unity.com/download) · [Guide d’installation Windows](https://docs.unity.com/en-us/hub/install-hub-win-mac)
2. Dans **Installs → Install Editor**, choisissez **Unity `6000.6.0f1`**, la version indiquée dans le projet. Si elle n’apparaît pas, ouvrez l’onglet **Archive**, recherchez `6000.6.0f1` et cliquez sur le lien d’installation avec Unity Hub. Autorisez le navigateur à ouvrir le Hub. [Archive Unity](https://unity.com/releases/editor/archive) · [Version utilisée par CercleVR](ProjectSettings/ProjectVersion.txt)
3. Dans la fenêtre des modules, cochez **Android Build Support**. Dépliez-le et cochez ses deux sous-modules **Android SDK & NDK Tools** et **OpenJDK**. Cliquez sur **Install** et attendez la fin du téléchargement. Si cette version d’Unity est déjà installée, ouvrez **Installs → Manage** (roue dentée ou menu à côté de la version) **→ Add modules** pour ajouter les éléments manquants. [Modules Android pour Unity](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/android/getting-started/sdksetup/install-dependencies)
4. Dans **Projects → Add → Add project from disk**, sélectionnez `INFO4900-Placeholder`. Ouvrez-le avec `6000.6.0f1` et laissez Unity terminer l’importation et la compilation.
5. Unity installe automatiquement **OpenXR Plugin 1.18.0**, **XR Interaction Toolkit 3.6.1** et **XR Plug-in Management 4.7.0**. Pour vérifier leurs versions, ouvrez **Window → Package Management → Package Manager → In Project**. Les *Starter Assets* sont déjà inclus et reliés à la scène. [Paquets du projet](Packages/manifest.json) · [Versions résolues](Packages/packages-lock.json)

## 3. Compiler le projet en APK

1. Dans l’éditeur Unity, ouvrez **File → Build Profiles**. Dans la liste des profils enregistrés, sélectionnez **Meta Quest**, cliquez sur **Switch Profile** et attendez l’indication **Active**.
2. Vérifiez la **Scene List** de ce profil. La seule scène cochée doit être `Assets/CercleVR/Scenes/CercleQuest.unity`. Le profil est déjà réglé pour **Android, IL2CPP et ARM64**. [Profil Meta Quest du projet](Assets/Settings/Build%20Profiles/Meta%20Quest.asset)
3. Ouvrez **Edit → Project Settings → XR Plug-in Management**. Sélectionnez l’onglet **Android** (icône du robot), vérifiez que **OpenXR** est coché, puis ouvrez la sous-page **OpenXR** en restant sur Android.
4. Sous **OpenXR Feature Groups**, activez **Meta Quest Support**. Cliquez sur la roue dentée à côté et vérifiez que **Quest 3** est coché dans **Target Devices**. Dans **Interaction Profiles**, utilisez **+** pour ajouter les profils manquants, **Oculus Touch Controller Profile** pour les commandes de base et **Meta Quest Touch Plus Controller Profile** pour les manettes du Quest 3. Sur la révision `c16a7ca`, le support Meta et ces deux profils sont désactivés après clonage. [Réglages OpenXR d’Unity](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.18/manual/features/metaquest.html) · [Suivi du problème](https://github.com/Narani-bit/INFO4900-Placeholder/issues/14)
5. Ouvrez **XR Plug-in Management → Project Validation**, sélectionnez **Android** et corrigez les erreurs indiquées. Enregistrez avec **Ctrl+S**, quittez le mode **Play** s’il est actif et attendez la fin de la compilation.
6. Dans la barre de menus d’Unity, choisissez **Cercle VR → Build → Quest - Developpement**. Le projet crée le dossier de sortie. Quand Unity annonce la réussite de la compilation, vous trouverez **l’APK à installer sur le casque** à cet emplacement dans le projet.

   ```text
   Builds\Quest-Development\CercleQuest.apk
   ```

   Pour ouvrir son dossier dans l’Explorateur depuis PowerShell, exécutez `explorer.exe .\Builds\Quest-Development`. Le fichier voisin `CercleQuest.apk.report.json` contient le résultat de la compilation et le nombre d’erreurs. [Script qui génère l’APK](Assets/CercleVR/Editor/CercleProfileBuild.cs)

## 4. Ajouter un profil sur le Quest 3

1. Mettez le casque et ouvrez le **profil du propriétaire**. La personne qui utilisera le nouveau profil doit disposer de son propre compte Meta.
2. Appuyez sur le **bouton Meta de la manette droite**, puis ouvrez **Paramètres → Profils**. Si votre menu affiche une horloge, sélectionnez-la pour ouvrir **Paramètres rapides**, puis la roue dentée **Paramètres**.
3. Suivez la procédure affichée par votre casque.

   | Ce qui apparaît dans **Profils** | Comment ajouter la personne |
   | --- | --- |
   | **Ajouter un profil**, puis un code à huit chiffres | Sélectionnez **Ajouter un profil**. Sur son téléphone, la personne ouvre l’adresse indiquée dans le casque, se connecte avec **son propre compte Meta** et saisit le code affiché. |
   | Un champ pour une **adresse courriel** | Saisissez l’adresse de la personne et envoyez l’invitation. Elle ouvre le courriel et accepte l’invitation avec **son propre compte Meta** dans les 15 minutes. |

4. Suivez les dernières indications dans le casque. Au démarrage suivant, choisissez le **nouveau profil** dans la liste. Si votre nom y figure déjà, il suffit de le sélectionner. [Aide Meta en français pour l’ajout par code](https://www.meta.com/fr-ca/help/quest/195886835457001/) · [Aide Meta pour l’invitation par courriel](https://www.meta.com/help/quest/195886835457001/)

## 5. Installer et tester le jeu

### Relier le casque au PC avec Meta VR Developer Hub

Si le Quest 3 apparaît déjà **Active** dans **Device Manager**, passez à [Installer l’APK sur le casque](#installer-lapk-sur-le-casque).

1. Sur Windows, installez **Meta VR Developer Hub** (aussi nommé *Meta Quest Developer Hub*). Ouvrez-le avec le compte Meta développeur utilisé pour connecter ce casque, puis sélectionnez **Device Manager**. [Télécharger Developer Hub pour Windows](https://developers.meta.com/vr/downloads/package/oculus-developer-hub-win/)
2. Installez le pilote **Oculus ADB Drivers** pour Windows. Décompressez le ZIP, faites un clic droit sur le fichier `.inf` puis choisissez **Installer**. Sous Windows 11, cette commande peut se trouver dans **Afficher plus d’options**. ADB permet au PC d’installer l’APK sur le casque. [Télécharger le pilote Meta](https://developers.meta.com/vr/downloads/package/oculus-adb-drivers/)
3. Branchez le Quest 3 au PC avec un **câble USB-C qui transmet les données**. Sur un PC utilisé pour la première fois, demandez au propriétaire du casque d’ouvrir son profil. À la question **Autoriser le débogage USB ?**, cochez **Toujours autoriser depuis cet ordinateur** si c’est votre PC, puis choisissez **Autoriser**. Revenez à **Device Manager** et vérifiez que le Quest 3 affiche **Active** en vert. [Connecter le casque à Developer Hub](https://developers.meta.com/vr/documentation/unity/ts-mqdh-getting-started/)
4. **Pour continuer en Wi-Fi**, connectez le PC et le casque au même réseau. Pendant que le câble est encore branché, ouvrez **Device Manager → Device Actions → ADB over Wi-Fi** et activez l’option. Attendez **Enabled**, débranchez le câble et vérifiez que le casque est toujours actif. Si cette connexion Wi-Fi est déjà établie, vous pouvez installer l’APK directement. [Utiliser ADB en Wi-Fi](https://developers.meta.com/vr/documentation/unity/ts-mqdh-basic-usage/)

### Installer l’APK sur le casque

1. Sur le PC, dans **Meta VR Developer Hub → Device Manager**, sélectionnez le Quest 3 marqué **Active**. Dans **Apps**, cliquez sur **Add Build** et choisissez `CercleQuest.apk`. Vous pouvez aussi glisser l’APK dans cette zone. Attendez la fin de l’installation. [Installer un APK avec Developer Hub](https://developers.meta.com/vr/documentation/unity/ts-mqdh-deploy-build/)
2. Dans le casque, ouvrez le profil avec lequel vous souhaitez tester le jeu. Appuyez sur le **bouton Meta de la manette droite**, ouvrez **Bibliothèque d’applications → Sources inconnues** (*Unknown Sources*) et lancez **CercleVR**. L’identifiant affiché dans certains menus est `com.cerclevr.game`.
3. Faites ce premier essai dans la salle d’enquête.

   | Action | Résultat attendu |
   | --- | --- |
   | Tournez la tête. | La salle et le cercle de suspects restent à leur place pendant que votre regard se déplace. |
   | Pointez un suspect ou son jeton et pressez le bouton latéral **Grip**, sous le majeur. | Le dialogue du suspect s’ouvre. |
   | Appuyez sur **X** sur la manette gauche, puis sélectionnez **Lire** sur le carnet. | Le carnet revient devant vous. **Lire** le fixe dans la salle pour consulter les notes. |

Pour essayer une modification, recompilez avec **Quest - Developpement**, installez le nouvel APK par **Add Build** et relancez **CercleVR**.

## Dépannage

| Problème | Vérification à faire |
| --- | --- |
| Unity demande le SDK, le NDK ou Java. | Dans **Unity Hub → Installs**, ajoutez les trois modules Android à **6000.6.0f1**. Dans l’éditeur, ouvrez **Edit → Preferences → External Tools → Android** et sélectionnez les outils fournis avec Unity. |
| Le menu **Cercle VR** manque ou aucun APK n’est généré. | Ouvrez **Window → General → Console** et lisez la première erreur rouge. Vérifiez que **Meta Quest** est actif, que l’importation est terminée et que vous avez quitté **Play**. |
| Le Quest 3 est absent de **Device Manager**. | Réveillez le casque, vérifiez que le pilote ADB est installé, puis essayez un autre port ou câble de données. Remettez le casque pour voir et accepter la demande de **débogage USB**. |
| L’APK est installé, mais **CercleVR** est absent du profil ajouté. | Vérifiez **Bibliothèque d’applications → Sources inconnues** sur le profil du propriétaire. Si le jeu y apparaît, lancez-le depuis ce profil pour vérifier l’installation. |
| Le jeu s’ouvre en fenêtre plate ou les manettes ne sélectionnent rien. | Revérifiez **Meta Quest Support** et les profils **Touch** dans l’onglet **Android** d’OpenXR. Recompilez, réinstallez le nouvel APK et relancez le jeu depuis **Sources inconnues**. |
