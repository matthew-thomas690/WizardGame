const fs = require("fs/promises");
const path = require("path");

async function copyFile(src, dest) {
  await fs.mkdir(path.dirname(dest), { recursive: true });
  await fs.copyFile(src, dest);
}

async function main() {
  const root = path.resolve(__dirname, "..");
  const nodeModules = path.join(root, "node_modules");

  const pixiSrc = path.join(nodeModules, "pixi.js", "dist", "pixi.min.js");
  const pixiDest = path.join(root, "wwwroot", "lib", "pixi", "pixi.min.js");

  const bootstrapSrc = path.join(nodeModules, "bootstrap", "dist", "css", "bootstrap.min.css");
  const bootstrapDest = path.join(root, "wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap.min.css");

  await copyFile(pixiSrc, pixiDest);
  await copyFile(bootstrapSrc, bootstrapDest);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
