import {PagedResult} from './api-client';

export function validatePagedResult<T>(
    page: PagedResult<T> | null | undefined,
    fallbackPageNumber: number,
    fallbackPageSize: number,
    message: string,
): PagedResult<T> {
    if (!page || !Array.isArray(page.items) || !Number.isInteger(page.totalCount) || page.totalCount < 0) {
        throw new Error(message);
    }

    return {
        items: page.items,
        totalCount: page.totalCount,
        pageNumber: Number.isInteger(page.pageNumber) ? page.pageNumber : fallbackPageNumber,
        pageSize: Number.isInteger(page.pageSize) ? page.pageSize : fallbackPageSize,
    };
}

export function listParams(
    pageNumber: number,
    pageSize: number,
    search: string,
): Record<string, string | number> {
    const params: Record<string, string | number> = {pageNumber, pageSize};
    if (search) params['search'] = search;
    return params;
}
