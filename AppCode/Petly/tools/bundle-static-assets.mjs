import fs from 'node:fs';
import path from 'node:path';

const projectRoot = process.cwd();
const configPath = path.join(projectRoot, 'bundleconfig.json');
const bundles = JSON.parse(fs.readFileSync(configPath, 'utf8'));

function stripCssComments(content) {
  return content.replace(/\/\*[\s\S]*?\*\//g, '');
}

function minifyCss(content) {
  return stripCssComments(content)
    .replace(/\s+/g, ' ')
    .replace(/\s*([{}:;,>])\s*/g, '$1')
    .replace(/;}/g, '}')
    .trim();
}

function minifyJs(content) {
  return content
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line && !line.startsWith('//'))
    .join('\n');
}

function bundleFiles(bundle) {
  const outputPath = path.join(projectRoot, bundle.outputFileName);
  const input = bundle.inputFiles
    .map((fileName) => fs.readFileSync(path.join(projectRoot, fileName), 'utf8'))
    .join('\n\n');

  const extension = path.extname(outputPath).toLowerCase();
  const shouldMinify = bundle.minify?.enabled === true;
  let output = input;

  if (shouldMinify && extension === '.css') {
    output = minifyCss(input);
  } else if (shouldMinify && extension === '.js') {
    output = minifyJs(input);
  }

  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  fs.writeFileSync(outputPath, `${output}\n`, 'utf8');
}

bundles.forEach(bundleFiles);
