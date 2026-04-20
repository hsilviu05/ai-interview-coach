import { mkdirSync, writeFileSync } from 'node:fs';
import path from 'node:path';

const scriptDirectory = path.dirname(new URL(import.meta.url).pathname);
const frontendRoot = path.resolve(scriptDirectory, '..');
const repoRoot = path.resolve(frontendRoot, '..');
const sourceUrl =
  process.env.AIC_OPENAPI_URL ?? 'http://127.0.0.1:5291/swagger/v1/swagger.json';
const outputPath = path.join(repoRoot, 'backend', 'openapi-v1.json');

const response = await fetch(sourceUrl);
if (!response.ok) {
  throw new Error(`Failed to download OpenAPI schema from ${sourceUrl}: ${response.status}`);
}

const schema = await response.text();
mkdirSync(path.dirname(outputPath), { recursive: true });
writeFileSync(outputPath, schema, 'utf8');
console.log(`Saved ${path.relative(repoRoot, outputPath)}`);
