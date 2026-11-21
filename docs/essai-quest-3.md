# Essai de CercleVR sur Quest 3

Version d’essai du 20 novembre 2026, construite avec Unity 6000.6.0f1. L’APK corrigé porte le nom `CercleQuest.apk` et la version Android 0.1.0. Son empreinte SHA-256 est `0529ed4ef4d7a6cd1224c36daed62766a0eb2956415a295fcc41659010f32f67`.

L’APK d’essai est disponible dans les fichiers de la [version Quest du 20 novembre 2026](https://github.com/Narani-bit/INFO4900-Placeholder/releases/tag/quest-2026-11-20). Il utilise une signature Android de test.

## Résultats du 20 novembre 2026

- **Unity, scène Quest.** Depuis le départ, les rayons simulés atteignent les sept personnages du premier niveau, leurs plaques et leurs jetons. Les essais vérifient les deux gâchettes, la poignée, le changement de page et le retour du carnet à 1,35 m avec X après un quart de tour.
- **Campagne.** Les six niveaux ont été vérifiés en mode Play dans Unity sur les scènes Quest et PC. La téléportation et la rotation par crans ont aussi été testées dans la scène Quest.
- **Quest 3.** Installation USB et démarrage confirmés avec l’APK ci-dessus. Le fichier installé correspond à l’empreinte de la version publiée. La compilation a abouti avec zéro erreur et trois avertissements de rendu et de configuration. Le journal de lancement du processus ne signale aucune erreur Unity ou Android Runtime.
- **À confirmer en jeu dans le casque.** Suivi de chaque manette, confort et lisibilité du carnet, sélection des personnages et des jetons, téléportation et passage au niveau 2.

## Trois essais à faire en premier

| Essai | Geste | Qu’as-tu observé ? |
|---|---|---|
| Arrivée | Garde les mains dans une position naturelle. Regarde devant toi, puis vers la main gauche. | Le petit carnet te laisse-t-il voir la salle ? Quand tu l’ouvres, le texte est-il confortable à lire ? |
| Personnages | Depuis le départ, vise un personnage avec la manette droite et presse la gâchette avant. Essaie ensuite sa plaque et un jeton numéroté. Refais un essai avec la main gauche. | Quel nom s’affiche dans le dialogue ? Pour un clic manqué, note la main, la cible et ce que le rayon touchait. |
| Rappel après un clic | Appuie sur **X à gauche**, ouvre **Aide**, puis **Page +**. Tourne-toi d’un quart de tour et appuie une seule fois sur **X**. Sélectionne **Réduire**, puis appuie encore sur **X**. | Le carnet revient-il devant toi à chaque pression ? La page reste-t-elle ouverte ? Si le carnet reste sur place, quel était le dernier bouton utilisé ? |

Pour un échec, donne le niveau, la main, le bouton et le résultat visible. Si tu filmes, ajoute le temps dans la vidéo. Ces essais correspondent aux tickets de démarrage, de suivi et de sélection. [Démarrage sur Quest #5](https://github.com/Narani-bit/INFO4900-Placeholder/issues/5) · [Suivi et commandes #14](https://github.com/Narani-bit/INFO4900-Placeholder/issues/14) · [Sélection #17](https://github.com/Narani-bit/INFO4900-Placeholder/issues/17)

## Commandes des manettes

| Geste | Action dans le jeu |
|---|---|
| Viser puis presser la gâchette avant ou la poignée latérale, sur l’une ou l’autre manette | Sélectionner un personnage, un nom, un jeton ou un bouton. Viser la couverture du petit carnet permet aussi de l’ouvrir. |
| X sur la manette gauche | Rappeler le carnet en grand à 1,35 m devant soi, même après un déplacement ou un changement de page. |
| Viser **Réduire** et presser la gâchette | Ramener le petit carnet à la main gauche. |
| Pousser un joystick vers l’avant, viser le sol et le relâcher | Se téléporter à l’endroit visé. La poignée de la même main annule la visée. |
| Donner un coup de joystick sur le côté ou vers l’arrière | Tourner de 45° sur le côté ou faire demi-tour. |
| A sur la manette droite | Sauter. |

## Pour les autres tickets qui demandent le casque

| Quand | Quoi noter | Tickets |
|---|---|---|
| Pendant une partie | Le jeu s’ouvre-t-il en VR ? Les deux rayons suivent-ils les mains ? Après avoir retiré et remis le casque, les clics fonctionnent-ils ? Noter aussi le résultat de **Marquer**, **Accuser**, **Annuler** et du passage au niveau suivant. | [Premier lancement #5](https://github.com/Narani-bit/INFO4900-Placeholder/issues/5) · [Tests de jeu #11](https://github.com/Narani-bit/INFO4900-Placeholder/issues/11) · [OpenXR #14](https://github.com/Narani-bit/INFO4900-Placeholder/issues/14) |
| En lisant le journal et un dialogue | Note le mot ou la ligne difficile à lire, la distance approximative, les contours qui scintillent et le texte coupé. Une capture de la même page permet de comparer deux versions. | [Lisibilité #7](https://github.com/Narani-bit/INFO4900-Placeholder/issues/7) · [Qualité d’image #3](https://github.com/Narani-bit/INFO4900-Placeholder/issues/3) |
| Pendant une mesure avec OVR Metrics Tool | Sur les niveaux 1, 3 et 6, garder le même parcours 3 minutes par niveau. Conserver le CSV de trois passages avec les FPS moyens et minimaux, les images perdues, le temps GPU, les niveaux CPU/GPU, la mémoire et la température disponibles. | [Protocole #9](https://github.com/Narani-bit/INFO4900-Placeholder/issues/9) |
| Pendant une session de 15 à 20 minutes | Noter chaque minute les mesures disponibles. Si l’image saccade, indiquer la minute, le niveau et l’action. Noter la chaleur ressentie, tout avertissement et la mémoire au premier puis au deuxième passage au niveau 1. | [Session longue #10](https://github.com/Narani-bit/INFO4900-Placeholder/issues/10) |
| Lorsqu’une version de comparaison est disponible | Refaire la même action au même niveau. Pour le journal, noter la saccade à l’ouverture ou au changement de page. Pour le rendu, comparer le temps GPU et l’aspect du texte, des bords et du plafond. Pour la téléportation, noter les saccades pendant la visée. | [Réglages d’image #3](https://github.com/Narani-bit/INFO4900-Placeholder/issues/3) · [Journal #8](https://github.com/Narani-bit/INFO4900-Placeholder/issues/8) · [Rendu Quest #18](https://github.com/Narani-bit/INFO4900-Placeholder/issues/18) · [Éclairage #20](https://github.com/Narani-bit/INFO4900-Placeholder/issues/20) · [Téléportation #21](https://github.com/Narani-bit/INFO4900-Placeholder/issues/21) |
| À la remise finale | Jouer 10 minutes avec l’APK exact de la remise. Noter son empreinte, les actions essayées et tout blocage. | [Validation finale #13](https://github.com/Narani-bit/INFO4900-Placeholder/issues/13) |
