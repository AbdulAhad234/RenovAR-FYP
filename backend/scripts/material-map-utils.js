const path = require("path");

const SUPPORTED_TEXTURE_EXTENSIONS = new Set([".jpg", ".jpeg", ".png", ".webp"]);

const normalizeRole = (role) => {
  const value = String(role || "").trim();
  if (!value) return null;

  const normalized = value.toLowerCase();
  if (normalized === "basecolor" || normalized === "albedo" || normalized === "diffuse") {
    return "baseColor";
  }
  if (normalized === "ao") return "ambientOcclusion";
  if (normalized === "nrm") return "normal";
  if (normalized === "disp" || normalized === "bump") return "height";
  if (normalized === "refl") return "reflection";
  if (normalized === "gloss") return "glossiness";
  return value;
};

const getMaterialMapRole = (filePath) => {
  const fileName = path.basename(filePath).toLowerCase();

  if (fileName.endsWith(".mtlx")) return null;
  if (fileName.includes("basecolor")) return "baseColor";
  if (/_col(?:_|\.|-)/i.test(fileName) || fileName.includes("_col_")) return "baseColor";
  if (fileName.includes("albedo") || fileName.includes("diffuse")) return "baseColor";
  if (fileName.includes("ambientocclusion") || /_ao(?:_|\.|-)/i.test(fileName)) {
    return "ambientOcclusion";
  }
  if (fileName.includes("normal") || /_nrm(?:_|\.|-)/i.test(fileName)) return "normal";
  if (fileName.includes("roughness")) return "roughness";
  if (fileName.includes("metallic") || fileName.includes("metalness")) return "metallic";
  if (fileName.includes("gloss")) return "glossiness";
  if (fileName.includes("reflection") || /_refl(?:_|\.|-)/i.test(fileName)) return "reflection";
  if (fileName.includes("displacement") || /_disp(?:16)?(?:_|\.|-)/i.test(fileName)) return "height";
  if (fileName.includes("bump")) return "height";

  return null;
};

const roleRank = (filePath) => {
  const extension = path.extname(filePath).toLowerCase();
  const name = path.basename(filePath).toLowerCase();
  let score = 0;

  if (SUPPORTED_TEXTURE_EXTENSIONS.has(extension)) score += 1000;
  if (extension === ".jpg" || extension === ".jpeg") score += 30;
  if (extension === ".png") score += 20;
  if (name.includes("_2k") || name.includes("2k_")) score += 10;
  if (name.includes("16")) score -= 50;

  return score;
};

const buildMaterialMaps = (textureFiles, toRelativePath = (value) => value) => {
  const byRole = new Map();

  for (const filePath of textureFiles) {
    const extension = path.extname(filePath).toLowerCase();
    const role = getMaterialMapRole(filePath);

    if (!role || !SUPPORTED_TEXTURE_EXTENSIONS.has(extension)) {
      continue;
    }

    const current = byRole.get(role);
    if (!current || roleRank(filePath) > roleRank(current)) {
      byRole.set(role, filePath);
    }
  }

  return Array.from(byRole.entries())
    .sort(([leftRole], [rightRole]) => leftRole.localeCompare(rightRole))
    .reduce((maps, [role, filePath]) => {
      maps[role] = toRelativePath(filePath);
      return maps;
    }, {});
};

module.exports = {
  SUPPORTED_TEXTURE_EXTENSIONS,
  buildMaterialMaps,
  getMaterialMapRole,
  normalizeRole,
};
