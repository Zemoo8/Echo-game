# ECHO : description du projet

**Chaque dash est une décision.**
ECHO est un jeu de plateforme 2D en pixel art, de science-fiction, fait avec Unity pour le navigateur et publié en WebGL sur GamePix.

![ECHO : écran titre](Screenshots/ui_menu.png)

## Fiche d'identité

| | |
|---|---|
| **Titre** | ECHO |
| **Genre** | Plateforme / action 2D, pixel art, science-fiction |
| **Auteur** | Ahmed Baghouli |
| **Moteur** | Unity 6 (6000.2.7f2), Universal Render Pipeline (rendu 2D) |
| **Langage** | C# |
| **Plateforme** | Navigateur web (WebGL) via le plugin GamePix 6.4, sur ordinateur et téléphone |
| **Commandes** | Clavier, manette ou boutons tactiles à l'écran |
| **Contenu** | 3 niveaux, un boss (le Gardien) et une cinématique de fin |
| **Durée d'une partie** | Une dizaine de minutes pour une première partie (estimation) |
| **Code source** | <https://github.com/Zemoo8/Echo-game> |

## Le concept

Toute la conception part d'une seule idée. Le dash, le meilleur mouvement de l'héroïne, coûte de l'énergie, et l'énergie est rare.

- Le dash traverse les grands vides, brise les barrières d'énergie et détruit les machines.
- **Chaque dash coûte une charge**, et la combinaison en contient trois au maximum.
- **Chaque niveau se termine par une porte que seul un dash peut briser.**

Le joueur doit donc toujours garder une charge en réserve, et chaque obstacle pose la même question : dasher maintenant, ou garder la charge pour plus tard ?

### La boucle de jeu

1. **Explorer** : courir, sauter et ramasser des cellules d'énergie (une charge chacune, trois au maximum).
2. **Décider** : face à un vide, une barrière, une machine ou un verrou, dasher maintenant ou garder la charge ?
3. **Dépenser ou garder** : le dash traverse et détruit, mais les verrous du cœur s'ouvrent seulement si l'on *garde* assez de charges.
4. **Sortir** : la porte de sortie demande un dash. Sans charge, il faut revenir au dernier point de contrôle.

## L'histoire

Une jeune femme se réveille seule dans une capsule de cryogénie. Le centre de recherche est plongé dans le noir, mais ses systèmes de sécurité fonctionnent encore. Tout en bas, le réacteur qui maintient le centre en vie est en train de lâcher, gardé par la machine construite pour le protéger. Si elle apaise le cœur du réacteur, le centre pourra appeler à l'aide.

## Le contenu

| Niveau | Lieu | Ce que le joueur apprend et affronte |
|---|---|---|
| 1 · Éveil | Le laboratoire abandonné | Courir, sauter (appui long pour sauter plus haut), le premier dash, le dash en l'air au-dessus des grands vides, des lasers qui s'allument et s'éteignent en rythme, des points de contrôle. Chaque idée arrive avec une aide adaptée au clavier, à la manette ou au tactile. |
| 2 · Aile de sécurité | Des couloirs d'acier gardés | Des drones qui patrouillent et des tourelles qui tirent : un dash les détruit, mais dépense la charge. Des consoles ouvrent des volets blindés ou coupent les lasers. |
| 3 · Cœur énergétique | Le cœur du réacteur | Des plaques fissurées qui cèdent sous les pieds, et des verrous qui s'ouvrent seulement si l'on garde assez de charges. Ici, il faut économiser au lieu de dépenser. |
| Boss · Le Gardien | L'arène du réacteur | Un combat en trois phases (voir plus bas). |
| Fin | Le réacteur apaisé | Une cinématique : le cœur se calme, une balise de détresse part, un vaisseau de secours répond. |

| | |
|---|---|
| ![Niveau 1](Screenshots/gameplay_level1.jpg) | ![Niveau 2](Screenshots/gameplay_level2.jpg) |
| ![Niveau 3](Screenshots/gameplay_level3.jpg) | ![Le Gardien](Screenshots/gameplay_boss.jpg) |

### Le Gardien

- On le touche seulement **avec un dash, pendant que sa carapace est ouverte** (cœur cyan). Il faut **6 coups en 3 phases**, et chaque phase est plus rapide que la précédente.
- Un dash dans la carapace fermée (cœur rouge) rebondit et fait perdre la charge. Le toucher sans dasher tue.
- **Ses attaques :** une onde au sol à sauter, des rayons tombant du plafond (annoncés par une fine ligne), et des tirs depuis les murs.
- Une mort fait recommencer la phase en cours, jamais tout le combat. Les cellules d'énergie de l'arène reviennent 4 secondes après avoir été prises.

## Les commandes

| Action | Clavier | Manette | Tactile |
|---|---|---|---|
| Se déplacer | A / D ou ← / → | Stick gauche, croix | Boutons ◀ ▶ |
| Sauter (appui long = plus haut) | Espace, W, ↑ | Bouton A / ✕ | Bouton ⌃ |
| Dash | Shift, X, J | Bouton X / □, RB, RT | Bouton » |
| Pause | Échap, P | Start | Bouton ❚❚ |
| Revenir au dernier point de contrôle | R | Select | Pause puis Recommencer |

## Direction artistique et son

- **Pixel art** à 32 pixels par unité, avec une caméra « pixel perfect ». Chaque zone a son propre « kit » : tuiles, fond, lumière et musique (Laboratoire, Sécurité, Cœur, Réacteur).
- **Un code couleur lisible :** le **cyan** est l'énergie et tout ce qu'un dash peut briser, le **rouge** est le danger (lasers, vides, carapace fermée), le **vert** est la sécurité (points de contrôle).
- **Animation et effets :** l'héroïne est animée (repos, course, saut, chute, dash) et s'étire ou s'écrase selon le mouvement. Le dash laisse une traînée et des images rémanentes. Il y a des particules, de légers tremblements de caméra et un court arrêt sur image lors des gros impacts.
- **Ressources externes :** uniquement des packs gratuits sous licence **CC0** (domaine public) : Luis Zuno (ansimuz) pour les décors, l'héroïne et les ennemis, Kenney pour les sons, la police et les particules, et SRG774 pour les 7 musiques. Les tuiles, cellules, barrières, objets de jeu, le Gardien, l'interface et le logo sont dessinés par un script Python écrit pour le projet (`Tools/make_echo_art.py`). Chaque ressource et sa licence sont listées dans `ASSET_LICENSES.md`.

![Guide visuel](Screenshots/design_02.png)

## Architecture technique

- **Les systèmes persistants** (une seule copie pendant toute la session) : `GameManager` (états du jeu, pause), `SceneLoader` (transitions en fondu), `AudioManager`, `VfxManager` (effets réutilisés, sans allocation pendant le jeu), `GamePixManager` et l'interface de jeu (barre d'énergie, cartes de titre, aides, pause, boutons tactiles).
- **Une scène à la fois** : menu, niveaux, arène, fin. Le gameplay parle aux systèmes par des **événements statiques**, donc un niveau ne connaît ni les menus, ni l'audio, ni la publicité.
- **Trois interfaces portent les règles :** `IDashTarget` (ce qu'un dash peut toucher : barrière, machine, Gardien), `IRestorable` (ce qu'un point de contrôle remet en place après une mort) et `ISwitchable` (ce qu'une console commande : volet, laser).
- **Des données réglables sans code** (ScriptableObjects) : `PlayerTuning` (vitesse, saut, dash), les kits d'environnement, la bibliothèque audio et la bibliothèque d'effets.
- **Des niveaux générés par code :** des « usines » dans l'éditeur (`EchoLevelFactory`, `EchoUiFactory`, etc.) construisent chaque scène, prefab et ressource. Le projet est donc reproductible et chaque changement est visible dans l'historique Git.
- **Une jouabilité tolérante :** temps de coyote (0,10 s), mémoire du saut (0,12 s) et du dash (0,10 s), et un dash de longueur fixe compté en pas de physique (4,5 unités en 0,15 s).
- **Les phases de présentation :** un fichier de configuration choisit la phase montrée (1 Conception … 8 GamePix) et active seulement ses fonctionnalités. Rien n'est supprimé, tout est seulement désactivé. Le menu **ECHO > Presentation** de l'éditeur lance chaque phase en un clic.

### Diagrammes

![Diagramme de cas d'utilisation](Diagrams/use_case.png)

![Diagramme d'activité : déroulement d'une partie](Diagrams/activity_game_flow.png)

Les diagrammes d'architecture, de classes, des états du jeu, des états du Gardien, du flux des scènes et de l'économie d'énergie sont dans la présentation intégrée au jeu (Phase 2, captures `Screenshots/concept_07.png` à `concept_12.png`).

## Tests et qualité

- **52 tests automatisés** (Unity Test Framework) : 10 en mode Edit et 42 en mode Play.
- **Ce qu'ils vérifient :** l'énergie, le saut, le temps de coyote, la mémoire du saut, le dash, la limite d'un dash en l'air, les barrières, les cellules, l'interface (menu, carte de titre, pause, mort et retour, fin de niveau, réglages du son enregistrés), les présentations, chaque niveau et ses objets, et les règles du Gardien.
- **Des « bots » jouent comme un joueur** (courir, sauter, dasher, attendre qu'un laser s'éteigne) : ils terminent chaque niveau et battent le Gardien. Un dernier test enchaîne tout le jeu à travers les vraies transitions de scène.
- **Résultats au 25/10/2026 :** 51 tests sur 52 passent. Le test du jeu entier bloque au Gardien : après une mort en phase 2, le Gardien reprend avec la même attaque, et le bot, encore en train de remonter sur sa plateforme, refait la même erreur en boucle. Le bot du test du boss seul gagne, car il ne meurt pas en phase 2.
- **Testé dans le navigateur :** les premières étapes (M0 à M2) ont été construites en WebGL avec le plugin GamePix et jouées dans Chrome (60 images/s, aucune erreur bloquante).
- **La vidéo de présentation** est filmée dans le jeu lui-même : un test spécial fait jouer les niveaux et enregistre chaque image (HUD compris), puis un script monte la vidéo avec ses sous-titres et la musique du jeu.

## Publication sur GamePix

- **Le paquet :** le plugin GamePix produit un paquet `.gpx`, qu'on envoie depuis le tableau de bord MyGamePix.
- **Pause automatique :** le plugin met le jeu en pause (temps et son) pendant une publicité ou quand l'onglet est caché. Le menu pause du jeu respecte cet état.
- **Sauvegarde :** elle passe par `PlayerPrefs`, que GamePix enregistre dans le `localStorage` du navigateur (bouton Continuer, réglages du son).
- **Publicité :** une publicité peut s'afficher **entre deux niveaux**, jamais pendant le jeu (Phase 8 uniquement).

## Méthode de travail

Le projet a avancé par étapes, chacune présentée au professeur : Conception, Diagrammes, Design, Développement, Animation 2D, Tests, Produit final et GamePix. Chaque étape correspond à des commits réels dans Git (M0 fondations, M1 sensations de jeu, M2 refonte visuelle, M3 niveaux, M4 boss et fin), notés dans `PRESENTATION_PHASES.md`.

**Difficultés rencontrées et solutions :**

- **Une police trompeuse :** dans la police pixel (Kenney Future), le K et le X ressemblent au H (« BACK » se lisait « BACH »). Les mots de l'interface évitent donc ces lettres (« RETURN », « EFFECTS »), et les textes longs utilisent une autre police.
- **La pause de GamePix :** le plugin met le jeu en pause quand l'onglet est caché. Les tests dans le navigateur ont donc demandé une page de test qui simule un onglet visible.
- **Un dash inégal :** sa longueur variait avec la fréquence d'images. Il est maintenant compté en pas de physique, donc il est identique partout.
- **Des captures coupées :** la caméra « pixel perfect » n'affiche qu'un quart de l'image dans une texture. On la désactive pendant les captures, à une résolution qui est un multiple exact.

## Les livrables

| Livrable | Où le trouver |
|---|---|
| Vidéo de présentation (environ 2 min) | En téléchargement dans la [version 1.0.0](https://github.com/Zemoo8/Echo-game/releases/tag/v1.0.0) |
| Description du projet | Ce document (`Docs/DESCRIPTION.md`) |
| Dépôt GitHub | <https://github.com/Zemoo8/Echo-game> |
| GamePix | Page du jeu créée sur MyGamePix. Le paquet `Zemoo8_ECHO_1.0.0.gpx` est dans la [version 1.0.0](https://github.com/Zemoo8/Echo-game/releases/tag/v1.0.0) |
