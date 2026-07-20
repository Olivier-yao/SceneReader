// Génère des identifiants snake_case stables et uniques (format §7 "Conventions de nommage").

const RE_DIACRITIQUES = /[̀-ͯ]/g; // marques combinantes laissées par normalize("NFD")

export function slugifier(texte: string): string {
  const base = texte
    .normalize("NFD")
    .replace(RE_DIACRITIQUES, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
  return /^[a-z]/.test(base) ? base : `x_${base}`;
}

export function idUnique(souhaite: string, idsExistants: Iterable<string>): string {
  const existants = new Set(idsExistants);
  const base = slugifier(souhaite) || "id";
  if (!existants.has(base)) return base;
  let n = 2;
  while (existants.has(`${base}_${n}`)) n++;
  return `${base}_${n}`;
}

export function prochainIdScene(idsExistants: Iterable<string>): string {
  const existants = new Set(idsExistants);
  let n = 1;
  let id = `scene_${String(n).padStart(2, "0")}`;
  while (existants.has(id)) {
    n++;
    id = `scene_${String(n).padStart(2, "0")}`;
  }
  return id;
}
