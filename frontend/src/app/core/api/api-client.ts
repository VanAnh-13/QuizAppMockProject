import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import {API_CONFIG} from '../config/api.config';

export interface PagedResult<T> {
    readonly items: readonly T[];
    readonly totalCount: number;
    readonly pageNumber: number;
    readonly pageSize: number;
}

@Injectable({providedIn: 'root'})
export class ApiClient {
    private readonly http = inject(HttpClient);
    private readonly config = inject(API_CONFIG);

    get<T>(path: string, params: Record<string, string | number> = {}): Promise<T> {
        return firstValueFrom(this.http.get<T>(this.url(path), {params}));
    }

    post<T>(path: string, body: unknown = {}): Promise<T> {
        return firstValueFrom(this.http.post<T>(this.url(path), body));
    }

    put<T>(path: string, body: unknown): Promise<T> {
        return firstValueFrom(this.http.put<T>(this.url(path), body));
    }

    patch<T>(path: string, body: unknown): Promise<T> {
        return firstValueFrom(this.http.patch<T>(this.url(path), body));
    }

    delete<T>(path: string): Promise<T> {
        return firstValueFrom(this.http.delete<T>(this.url(path)));
    }

    async list<T>(path: string, params: Record<string, string | number> = {}): Promise<readonly T[]> {
        const items: T[] = [];

        let pageNumber = 1;

        while (true) {
            const page = await this.get<PagedResult<T>>(path, {
                ...params,
                pageNumber,
                pageSize: this.config.pageSize,
            });

            if (
                !Array.isArray(page.items) ||
                !Number.isInteger(page.totalCount) ||
                page.totalCount < 0 ||
                page.pageNumber !== pageNumber
            ) {
                throw new Error('Invalid pagination data.');
            }

            items.push(...page.items);

            if (items.length >= page.totalCount) return items;

            if (page.items.length === 0) throw new Error('The server returned an incomplete list.');

            pageNumber++;
        }
    }

    private url(path: string): string {
        return `${this.config.baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;
    }
}
