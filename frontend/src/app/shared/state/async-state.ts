export type AsyncState<T> =
    | { readonly status: 'idle' }
    | { readonly status: 'loading' }
    | { readonly status: 'success'; readonly data: T }
    | { readonly status: 'error'; readonly error: string };

export function idle<T>(): AsyncState<T> {
    return {status: 'idle'};
}

export function loading<T>(): AsyncState<T> {
    return {status: 'loading'};
}

export function success<T>(data: T): AsyncState<T> {
    return {status: 'success', data};
}

export function error<T>(message: string): AsyncState<T> {
    return {status: 'error', error: message};
}
