import {readdir, readFile} from 'node:fs/promises';
import {extname, join, relative, resolve} from 'node:path';
import {fileURLToPath} from 'node:url';

const ROOT_DIR = resolve(fileURLToPath(new URL('.', import.meta.url)), '..');
const FRONTEND_APP_DIR = join(ROOT_DIR, 'frontend', 'src', 'app');

const FORBIDDEN_CSS_VARS = [
    '--text-secondary',
    '--primary',
    '--surface',
    '--border-color',
    '--surface-hover',
    '--text-primary',
    '--error-text',
];

const FORBIDDEN_COLOR_PATTERNS = [
    {pattern: /#4f46e5/i, label: 'prohibited indigo #4f46e5'},
    {pattern: /#6756cd/i, label: 'prohibited purple #6756cd'},
    {pattern: /#4338ca/i, label: 'prohibited indigo #4338ca'},
    {pattern: /#6366f1/i, label: 'prohibited indigo #6366f1'},
    {pattern: /rgba?\s*\(\s*99\s*,\s*102\s*,\s*241/i, label: 'prohibited indigo rgb(99, 102, 241)'},
    {pattern: /#7060da/i, label: 'prohibited purple #7060da'},
    {pattern: /#eeebff/i, label: 'prohibited soft purple #eeebff'},
    {pattern: /\b(text|bg|border|ring)-(indigo|violet|purple)-\d+\b/, label: 'Tailwind purple/indigo/violet class'},
];

async function collectFiles(dir, extensions) {
    const results = [];
    try {
        const entries = await readdir(dir, {withFileTypes: true});
        for (const entry of entries) {
            const fullPath = join(dir, entry.name);
            if (entry.isDirectory()) {
                results.push(...(await collectFiles(fullPath, extensions)));
            } else if (entry.isFile() && extensions.includes(extname(entry.name))) {
                results.push(fullPath);
            }
        }
    } catch {
        // directory might not exist yet
    }
    return results;
}

let errorCount = 0;

function reportViolation(file, lineNum, rule, message) {
    errorCount++;
    const rel = relative(ROOT_DIR, file).replace(/\\/g, '/');
    console.error(`  ❌ [${rule}] ${rel}:${lineNum} - ${message}`);
}

async function checkDesignTokens(files) {
    console.log('🔍 Checking design tokens and colors (DESIGN.MD)...');
    for (const file of files) {
        if (file.endsWith('.spec.ts')) continue;
        const content = await readFile(file, 'utf-8');
        const lines = content.split('\n');

        lines.forEach((line, index) => {
            const lineNum = index + 1;

            // Check forbidden CSS variable usage: var(--token) or declaration: (^|[;{\s])--token:
            for (const token of FORBIDDEN_CSS_VARS) {
                const varUsageRegex = new RegExp(`var\\(\\s*${token}\\s*[,)]`);
                const varDeclRegex = new RegExp(`(?:^|[;{\\s])${token}\\s*:`);
                if (varUsageRegex.test(line) || varDeclRegex.test(line)) {
                    reportViolation(file, lineNum, 'DESIGN_TOKEN', `Uses non-existent/prohibited CSS variable "${token}"`);
                }
            }

            // Check forbidden color codes & Tailwind classes
            for (const {pattern, label} of FORBIDDEN_COLOR_PATTERNS) {
                if (pattern.test(line)) {
                    reportViolation(file, lineNum, 'BRAND_COLOR', `Uses ${label}`);
                }
            }
        });
    }
}

async function checkComponentStructure(tsFiles) {
    console.log('🔍 Checking Angular component structure (separate ts/html/css)...');
    for (const file of tsFiles) {
        if (file.endsWith('.spec.ts')) continue;
        const content = await readFile(file, 'utf-8');

        if (content.includes('@Component(')) {
            // Check for inline template
            if (/\btemplate\s*:\s*[`'"]/.test(content)) {
                reportViolation(file, 1, 'COMPONENT_SEPARATION', 'Component uses inline "template". Must use "templateUrl".');
            }
            // Check for inline styles
            if (/\bstyles\s*:\s*\[\s*[`'"]/.test(content) || /\bstyle\s*:\s*[`'"]/.test(content)) {
                reportViolation(file, 1, 'COMPONENT_SEPARATION', 'Component uses inline "styles". Must use "styleUrl" or "styleUrls".');
            }
        }
    }
}

async function checkStrictTyping(tsFiles) {
    console.log('🔍 Checking strict typing (no `any`)...');
    for (const file of tsFiles) {
        if (file.endsWith('.spec.ts') || file.endsWith('.d.ts')) continue;
        const content = await readFile(file, 'utf-8');
        const lines = content.split('\n');

        lines.forEach((line, index) => {
            const lineNum = index + 1;
            const trimmed = line.trim();
            // Skip comments
            if (trimmed.startsWith('//') || trimmed.startsWith('/*') || trimmed.startsWith('*')) return;

            // Match ": any", "as any", "<any>"
            if (/:\s*any\b/.test(line) || /\bas\s+any\b/.test(line) || /<any>/.test(line)) {
                reportViolation(file, lineNum, 'NO_ANY', 'Prohibited "any" type detected. Use strict types or unknown.');
            }
        });
    }
}

async function checkLayerBoundaries(tsFiles) {
    console.log('🔍 Checking architecture layer boundaries (Hexagonal)...');
    for (const file of tsFiles) {
        if (file.endsWith('.spec.ts')) continue;
        const rel = relative(FRONTEND_APP_DIR, file).replace(/\\/g, '/');
        const content = await readFile(file, 'utf-8');
        const lines = content.split('\n');

        lines.forEach((line, index) => {
            const lineNum = index + 1;
            if (!line.includes('import ')) return;

            // Core should never import from feature presentation or infrastructure
            if (rel.startsWith('core/') && (line.includes('/presentation/') || line.includes('/infrastructure/'))) {
                reportViolation(file, lineNum, 'LAYER_BOUNDARY', 'Core layer must not depend on feature presentation or infrastructure.');
            }

            // Domain should never import from Angular framework or HTTP
            if (rel.includes('/domain/') && (line.includes('@angular/') || line.includes('@angular/common/http'))) {
                reportViolation(file, lineNum, 'LAYER_BOUNDARY', 'Domain layer must be pure TypeScript without Angular dependencies.');
            }

            // Presentation components should not inject HttpClient directly
            if (rel.includes('/presentation/') && line.includes('@angular/common/http') && line.includes('HttpClient')) {
                reportViolation(file, lineNum, 'RAW_HTTP_CLIENT', 'Presentation layer must not use HttpClient directly. Use ApiClient or ports.');
            }
        });
    }
}

async function main() {
    console.log('====================================================');
    console.log('🛡️  QuizApp Agent Rule & Architecture Harness');
    console.log('====================================================\n');

    const appFiles = await collectFiles(FRONTEND_APP_DIR, ['.ts', '.html', '.css']);
    const tsFiles = appFiles.filter(f => f.endsWith('.ts'));

    await checkDesignTokens(appFiles);
    await checkComponentStructure(tsFiles);
    await checkStrictTyping(tsFiles);
    await checkLayerBoundaries(tsFiles);

    console.log('\n----------------------------------------------------');
    if (errorCount === 0) {
        console.log(`✅ All checks PASSED! Total files scanned: ${appFiles.length}`);
        console.log('====================================================\n');
        process.exit(0);
    } else {
        console.error(`❌ Rule verification FAILED with ${errorCount} violation(s).`);
        console.log('====================================================\n');
        process.exit(1);
    }
}

main().catch(err => {
    console.error('Fatal error running harness:', err);
    process.exit(1);
});
