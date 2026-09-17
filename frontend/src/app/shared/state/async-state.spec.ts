import {error, idle, loading, success} from './async-state';

describe('async-state factory helpers', () => {
    it('creates idle state', () => {
        expect(idle()).toEqual({status: 'idle'});
    });

    it('creates loading state', () => {
        expect(loading()).toEqual({status: 'loading'});
    });

    it('creates success state with payload', () => {
        const data = {id: '123', title: 'Quiz'};
        expect(success(data)).toEqual({status: 'success', data});
    });

    it('creates error state with message', () => {
        expect(error('Something went wrong')).toEqual({
            status: 'error',
            error: 'Something went wrong',
        });
    });
});
