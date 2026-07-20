# Template Unity « Player » — Phase 2

Projet Unity (6000.5.4f1, URP) qui lit un fichier `.scenereader.json` (exporté
par l'app Phase 1, voir [`../app/`](../app/) et
[`../format/FORMAT_SCENEREADER.md`](../format/FORMAT_SCENEREADER.md)) et le
rend jouable : menu, dialogues, choix, variables/conditions, sauvegarde.

## Ouvrir le projet

1. Ouvrez **Unity Hub** → **Ouvrir** → sélectionnez le dossier
   `unity_template/`.
2. Unity régénère `Library/` au premier lancement (peut prendre 1 à 2
   minutes) — c'est normal, ce dossier n'est pas versionné.
3. Dans la fenêtre **Project**, ouvrez `Assets/SceneReader/Scenes/MenuPrincipal.unity`.
4. Cliquez sur **▶ Play** en haut de l'éditeur.

Vous devriez voir le menu principal lister **« La Porte »** (l'histoire
d'exemple, déjà déposée dans `Assets/StreamingAssets/Histoires/`). Cliquez
dessus, puis **Nouvelle partie**, et jouez : narration/dialogues au clic,
choix par boutons, touche **H** pour l'historique, bouton **Sauvegarder**
pour choisir un des 3 emplacements.

## Déposer votre propre histoire

Copiez le `.scenereader.json` exporté par l'app Phase 1 (et les images
éventuelles) dans `Assets/StreamingAssets/Histoires/`. Les chemins `decor` /
`portrait` du JSON sont résolus **depuis ce dossier** (ex. `decors/x.png` →
`Assets/StreamingAssets/Histoires/decors/x.png`). Relancez Play (ou rouvrez
le menu) : la nouvelle histoire apparaît dans la liste.

## Comment c'est construit

- `Assets/SceneReader/Scripts/Engine/` — moteur, indépendant de l'UI et de
  toute scène : modèle de données (`Modele/`), parseur JSON maison sans
  dépendance externe (`Json/`), variables, conditions (`==`, `!=`, `&&`,
  `||`), chargement des histoires, sauvegarde/chargement. Assemblage séparé
  (`SceneReader.Runtime.asmdef`) pour permettre des tests automatisés.
- `Assets/SceneReader/Scripts/UI/` — `MenuPrincipalControleur`,
  `JeuControleur`, `HistoriqueControleur`, `PanneauSauvegardeControleur` :
  s'abonnent aux événements du moteur (`SurElementAffiche`,
  `SurChangementScene`, `SurFinHistoire`), ne connaissent jamais le format
  JSON directement.
- `Assets/SceneReader/Scripts/Editor/SceneReaderSceneBuilder.cs` — construit
  les scènes `MenuPrincipal.unity` et `Jeu.unity` **par code** (Canvas, UI
  uGUI classique, câblage des références). Si vous modifiez la disposition
  de l'UI dans ce script, relancez-le pour régénérer les scènes :
  menu **SceneReader → Construire les scènes du player** dans l'éditeur, ou
  en ligne de commande :
  ```
  Unity.exe -batchmode -quit -projectPath unity_template ^
    -executeMethod SceneReader.EditeurOutils.SceneReaderSceneBuilder.ConstruireTout
  ```
- `Assets/SceneReader/Tests/Editor/HistoireDemoTests.cs` — tests automatisés
  (EditMode) qui rejouent l'histoire d'exemple de bout en bout et vérifient
  les 3 critères de validation Phase 2 ci-dessous. Pour les relancer :
  **Window → General → Test Runner** dans l'éditeur, onglet *EditMode*, ou en
  ligne de commande :
  ```
  Unity.exe -batchmode -projectPath unity_template ^
    -runTests -testPlatform EditMode -testResults resultats.xml
  ```

## Notes d'implémentation

- **uGUI classique** (`UnityEngine.UI.Text`/`Image`/`Button`), pas
  TextMeshPro : évite toute dépendance/import interactif supplémentaire,
  garde le template 100% reconstructible en ligne de commande. Passer à TMP
  plus tard est une migration simple si vous voulez une meilleure
  typographie (remplacer les `Text` par des `TMP_Text` dans les scripts UI
  et régénérer les scènes).
- **Input Manager (ancien)** plutôt que le package Input System : suffisant
  pour un point-and-click, évite une dépendance de package supplémentaire.
- **Chargement d'images au runtime** : `ChargeurImages.cs` lit les fichiers
  via `System.IO` + `ImageConversion.LoadImage`, ce qui fonctionne en
  standalone (PC/Mac/Linux). Une build **WebGL** (Phase 3) devra remplacer
  ça par `UnityWebRequestTexture`, StreamingAssets n'étant pas un vrai
  système de fichiers dans un navigateur.
- Modules moteur (`com.unity.modules.imageconversion`,
  `com.unity.modules.unitywebrequest`) déclarés explicitement dans
  `Packages/manifest.json` : sur cette version d'Unity ils ne sont pas
  référencés implicitement par défaut si aucun package tiers ne les tire
  transitivement.

## Critères de validation Phase 2 (cahier des charges)

Vérifiés automatiquement par `HistoireDemoTests.cs` (4/4 tests passent) :

- [x] Le JSON exporté en Phase 1 se joue de bout en bout sans erreur —
  `ChoixModifieVariableEtSceneUlterieureEnTientCompte` rejoue les 5 scènes
  de l'histoire d'exemple jusqu'à une fin.
- [x] Un choix modifie une variable et une scène ultérieure en tient compte —
  même test : le choix "Mentir" en scene_01 met `a_menti` à `true`, et
  l'option conditionnelle de scene_03 (`a_menti == true`) devient visible en
  conséquence. `ChoixAvecConditionFausseEstMasque` vérifie le cas inverse.
- [x] Sauvegarder, quitter, recharger : on reprend exactement où on était —
  `SauvegarderQuitterRechargerRepredExactementOuOnEtait` sauvegarde en cours
  de partie, recrée un contrôleur "à froid" (simulant un redémarrage), et
  vérifie que scène, position et variables sont restaurées à l'identique.

Ces tests couvrent le **moteur**. Le test manuel en Play Mode (section
"Ouvrir le projet" ci-dessus) reste nécessaire pour vérifier l'**UI** :
affichage du décor/portrait, défilement lettre à lettre, curseur de vitesse,
boutons de choix, panneau d'historique (H), panneau de sauvegarde à 3
emplacements — je ne peux pas piloter l'éditeur Unity en mode fenêtré depuis
cet environnement, donc cette partie n'a pas été vérifiée visuellement par
moi ; testez-la avant de considérer la Phase 2 close.
