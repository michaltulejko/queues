import { useEffect, useState } from 'react';
import { Observable } from 'rxjs';

export function useObservable<T>(observable: Observable<T>, initialValue: T): T {
    const [value, setValue] = useState<T>(initialValue);

    useEffect(() => {
        // Create a new subscription that only updates state when the component is mounted
        const subscription = observable.subscribe(newValue => {
            setValue(prev => {
                // Only update if value actually changed
                if (JSON.stringify(prev) !== JSON.stringify(newValue)) {
                    return newValue;
                }
                return prev;
            });
        });

        // Cleanup the subscription when component unmounts or observable changes
        return () => subscription.unsubscribe();
    }, [observable]); // Only re-subscribe when the observable reference changes

    return value;
}