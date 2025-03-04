import { Observable, from, forkJoin } from 'rxjs';
import { switchMap, map, catchError } from 'rxjs/operators';
import { MessageData } from "../types/messageData";

// List of queues to fetch data for
const QUEUE_TYPES = ['Kafka', 'SQS', 'Rabbit'];

export function fetchMessageData(recordsAmount: number = 10): Observable<MessageData[]> {
    // Create an array of observables for each queue fetch operation
    const queueObservables = QUEUE_TYPES.map(queueName => {
        return from(
            fetch(`https://localhost:7106/Queues/statistics?queueName=${queueName}&recordsAmount=${recordsAmount}`, {
                headers: {
                    'accept': '*/*'
                }
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`Error fetching data for ${queueName}: ${response.status}`);
                    }
                    return response.json();
                })
        ).pipe(
            // Add queue name if not included in the response
            map((data: any[]) =>
                data.map(item => ({
                    ...item,
                    queueName: item.queueName || queueName
                }))
            )
        );
    });

    // Wait for all queue requests to complete and combine results
    return forkJoin(queueObservables).pipe(
        map(results => {
            // Flatten array of arrays
            return results.flat();
        }),
        catchError(error => {
            console.error("API fetch error:", error);
            throw error;
        })
    );
}