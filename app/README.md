# SceneReader — app (Phase 1)

Application web locale (React + Vite + TypeScript) : convertit de la prose en
découpage de jeu narratif, éditable à la main, exportable en
`.scenereader.json` (voir [`../format/FORMAT_SCENEREADER.md`](../format/FORMAT_SCENEREADER.md)).

Aucun compte, aucun cloud : tout est stocké dans le navigateur (localStorage)
sur cette machine. Les seuls appels réseau sortants sont, si vous activez un
mode IA, vers l'API du fournisseur choisi (Groq/OpenRouter/Anthropic/Gemini) —
rien d'autre.

## Démarrer

```bash
npm install
npm run dev
```

Puis ouvrez l'URL affichée (par défaut http://localhost:5173).

## Scripts

- `npm run dev` — serveur de développement avec rechargement à chaud.
- `npm run build` — vérifie les types (`tsc --noEmit`) puis build de
  production dans `dist/`.
- `npm run preview` — sert le build de production localement.
- `npm run typecheck` — vérification TypeScript seule.

## Structure

```
src/
├── types/format.ts        types miroir du contrat .scenereader.json
├── providers/              adaptateurs IA (interface commune + 3 implémentations)
│   ├── types.ts             interface AdaptateurIA / OptionsAnalyse / erreurs
│   ├── orchestrerAnalyse.ts logique commune : chunking, backoff, retry JSON
│   ├── openaiCompatible.ts  Groq / OpenRouter / tout endpoint /chat/completions
│   ├── anthropic.ts         API Claude native
│   ├── gemini.ts            API Google AI Studio
│   └── index.ts             registre + factory
├── lib/                    fonctions pures (aucun état React)
│   ├── chunking.ts          découpage de texte long avec recouvrement
│   ├── prompt.ts             construction du prompt d'analyse
│   ├── parseReponseIA.ts    extraction/validation du JSON renvoyé par le modèle
│   ├── retry.ts              backoff exponentiel sur 429
│   ├── pricing.ts            table de prix éditable + estimation de coût
│   ├── validation.ts         règles d'intégrité du format (§6 du contrat)
│   ├── exportImport.ts       export/import du fichier .scenereader.json
│   └── id.ts                  génération d'identifiants snake_case uniques
├── state/                  état applicatif React
│   ├── config.ts             réglages (mode actif, clés API, prix personnalisés)
│   ├── project.ts            modèle de projet (texte source + histoire)
│   ├── storage.ts            persistance localStorage (config, projets)
│   └── histoireActions.ts    fonctions pures de mutation de l'Histoire
└── components/              UI (3 volets + réglages)
```

## Ajouter un nouveau fournisseur IA

1. Créer `src/providers/monFournisseur.ts` qui implémente `AdaptateurIA`
   (voir `src/providers/types.ts`) et utilise `analyserAvecAppel` de
   `orchestrerAnalyse.ts` pour bénéficier gratuitement du chunking, du retry
   429 et de la re-tentative sur JSON invalide.
2. L'enregistrer dans `src/providers/index.ts` (`ADAPTATEURS`).
3. Ajouter ses réglages par défaut dans `src/state/config.ts`
   (`configParDefaut`).
4. Ajouter son option dans `src/components/ReglagesModal.tsx`.

Aucune autre partie de l'app n'a besoin d'être modifiée.

## Note CORS (appels directs depuis le navigateur)

Cette app appelle les API IA **directement depuis le navigateur** (pas de
serveur intermédiaire), ce qui est acceptable puisqu'elle tourne uniquement
en local sur la machine de l'utilisateur. Deux points à savoir :

- **Anthropic** exige l'en-tête `anthropic-dangerous-direct-browser-access`
  (déjà envoyé par l'adaptateur) pour autoriser les appels navigateur.
- **Groq** et **Gemini** acceptent les appels navigateur nativement.
  **OpenRouter** aussi, mais certains endpoints "OpenAI-compatible" tiers
  peuvent bloquer les requêtes cross-origin (CORS) selon leur configuration —
  si un fournisseur personnalisé échoue avec une erreur réseau générique
  (pas 429, pas de réponse HTTP claire), c'est la cause la plus probable ;
  dans ce cas, utilisez un fournisseur qui autorise CORS ou repassez en mode
  manuel pour ce texte.

## Tester manuellement sans clé API

1. `npm run dev`, ouvrez l'app.
2. Réglages → laissez « Mode manuel (zéro API) » sélectionné (c'est le
   défaut).
3. Bouton **Importer** → choisissez [`../exemples/histoire_demo.scenereader.json`](../exemples/histoire_demo.scenereader.json).
4. Vous devez voir 5 scènes, 2 personnages (Amara, Le Gardien), 2 variables
   (`a_menti`, `porte_ouverte`), un embranchement conditionnel, deux fins.
5. Modifiez un élément (texte, personnage, condition), puis **Exporter** :
   le fichier doit se télécharger sans erreur.
6. Pour tester la détection d'erreur : supprimez la condition d'un choix ou
   videz le champ « scène cible » d'une option, puis tentez d'exporter — un
   bandeau rouge doit lister l'erreur précise (scène + raison).
