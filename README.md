# SceneReader

« Tu écris comme un écrivain, il compile comme un développeur. »

SceneReader transforme une histoire écrite en prose normale (sans balise ni
syntaxe spéciale) en jeu narratif jouable (visual novel). Vous collez votre
texte, l'app le découpe en scènes/dialogues/choix (à la main ou avec l'aide
d'une IA), vous corrigez, puis vous exportez un fichier `.scenereader.json`
prêt à être joué dans un template Unity.

## État du projet

- ✅ **Phase 1 — le convertisseur prose → JSON** : terminée, voir
  [`app/`](app/).
- ✅ **Phase 2 — le template Unity « Player »** : moteur + UI construits et
  testés automatiquement (voir [`unity_template/README.md`](unity_template/README.md)) ;
  reste à valider vous-même l'UI en Play Mode (affichage, défilement,
  vitesse) que je n'ai pas pu piloter visuellement depuis cet environnement.
- ⏳ **Phase 3 — éditeur de branches et diffusion** : pas encore commencée.

## Installation pas à pas (pour quelqu'un qui n'a jamais lancé d'app React)

### 1. Installer Node.js

SceneReader a besoin de **Node.js** (qui installe aussi `npm`, l'outil qui
télécharge les dépendances du projet).

1. Allez sur [nodejs.org](https://nodejs.org/) et téléchargez la version
   « LTS » (recommandée).
2. Lancez l'installeur, laissez les options par défaut.
3. Vérifiez l'installation : ouvrez une invite de commande (PowerShell sous
   Windows) et tapez :
   ```
   node -v
   npm -v
   ```
   Les deux commandes doivent afficher un numéro de version (pas d'erreur).

### 2. Récupérer le projet

Si vous avez ce dossier `scenereader/` sur votre machine, passez directement
à l'étape 3. Sinon, clonez ou téléchargez le dépôt puis ouvrez un terminal
dans le dossier `scenereader/`.

### 3. Installer les dépendances de l'app

Dans le terminal :
```
cd app
npm install
```
Cette commande télécharge les librairies nécessaires (React, Vite…) dans un
dossier `app/node_modules/` — c'est normal que ça prenne quelques dizaines de
secondes la première fois.

### 4. Lancer l'application

Toujours dans `app/` :
```
npm run dev
```
Le terminal affiche une adresse du type `http://localhost:5173/`. Ouvrez-la
dans votre navigateur (Chrome, Firefox, Edge…). L'application s'affiche —
c'est tout, pas d'installation supplémentaire, pas de compte à créer.

Pour arrêter le serveur : retournez dans le terminal et appuyez sur
`Ctrl+C`.

### 5. Premier essai (sans clé API)

1. Dans l'app, cliquez sur **Importer** (en haut à droite) et choisissez le
   fichier [`exemples/histoire_demo.scenereader.json`](exemples/histoire_demo.scenereader.json).
2. Vous voyez apparaître les scènes, personnages et variables de l'histoire
   d'exemple dans les panneaux « Découpage » et « Personnages ».
3. Modifiez ce que vous voulez, puis cliquez sur **Exporter
   .scenereader.json** : le fichier se télécharge dans votre dossier
   Téléchargements.

Ça fonctionne intégralement **sans aucune clé API**, en mode manuel.

### 6. (Optionnel) Activer l'analyse par IA

Si vous voulez que l'IA propose automatiquement un découpage de votre propre
texte plutôt que de tout faire à la main :

1. Cliquez sur **Réglages**.
2. Choisissez un mode :
   - **Groq** (gratuit) : créez un compte sur [console.groq.com](https://console.groq.com/keys)
     et générez une clé API.
   - **Google Gemini** (gratuit) : créez une clé sur
     [aistudio.google.com/apikey](https://aistudio.google.com/apikey).
   - **Anthropic** (payant, meilleure qualité) : créez une clé sur
     [console.anthropic.com](https://console.anthropic.com/).
3. Collez la clé dans le champ correspondant. Elle reste sur votre machine
   (stockage local du navigateur), n'est jamais envoyée ailleurs qu'au
   fournisseur choisi.
4. Collez votre texte dans le panneau « Texte source » et cliquez sur
   **Analyser**.

⚠️ Certains fournisseurs gratuits utilisent les textes envoyés pour
entraîner leurs modèles — vérifiez leurs conditions d'utilisation avant d'y
envoyer une histoire à laquelle vous tenez.

## Structure du dépôt

```
scenereader/
├── app/                          application web locale (Phase 1)
├── format/
│   └── FORMAT_SCENEREADER.md     spécification complète du JSON pivot
├── unity_template/               template Unity « Player » (Phase 2)
├── exemples/
│   └── histoire_demo.scenereader.json   histoire jouable pour tester sans API
└── README.md                     ce fichier
```

## Documentation

- [`format/FORMAT_SCENEREADER.md`](format/FORMAT_SCENEREADER.md) — le
  contrat entre l'app et le futur player Unity.
- [`app/README.md`](app/README.md) — documentation développeur de l'app
  (architecture des adaptateurs IA, scripts, comment tester).
- [`unity_template/README.md`](unity_template/README.md) — comment ouvrir le
  template Unity, y jouer l'histoire d'exemple, et ce qui a été vérifié
  automatiquement (tests) vs à tester vous-même en Play Mode.
