/**
 * Resolves an authentic course image URL from a database customUrl or topic keywords.
 */
export function resolveQuizImageUrl(title: string, customUrl?: string | null): string {
    if (customUrl) return customUrl;
    const lower = title.toLowerCase();
    if (lower.includes('angular')) {
        return 'https://images.unsplash.com/photo-1555066931-4365d14bab8c?q=80&w=600&auto=format&fit=crop';
    }
    if (lower.includes('c#') || lower.includes('.net') || lower.includes('csharp') || lower.includes('oop')) {
        return 'https://images.unsplash.com/photo-1517694712202-14dd9538aa97?q=80&w=600&auto=format&fit=crop';
    }
    if (lower.includes('sql') || lower.includes('database') || lower.includes('server')) {
        return 'https://images.unsplash.com/photo-1544383835-bda2bc66a55d?q=80&w=600&auto=format&fit=crop';
    }
    if (lower.includes('typescript') || lower.includes('types')) {
        return 'https://images.unsplash.com/photo-1516116211227-bbc00a58ad8c?q=80&w=600&auto=format&fit=crop';
    }
    return 'https://images.unsplash.com/photo-1516321318423-f06f85e504b3?q=80&w=600&auto=format&fit=crop';
}
