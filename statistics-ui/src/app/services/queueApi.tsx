import { Observable, from, forkJoin } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
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
            ),
            // Normalize timestamps per queue
            map(data => normalizeTimestampsPerQueue(data, queueName))
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

// Function to normalize timestamps for each queue separately
function normalizeTimestampsPerQueue(data: MessageData[], queueName: string): MessageData[] {
    if (data.length === 0) return data;

    // Find the earliest timestamp for this queue
    const earliestTimestamp = Math.min(...data.map(item => item.processingTime));

    // Normalize all timestamps relative to the earliest in this queue
    return data.map(item => ({
        ...item,
        originalTimeStamp: item.processingTime,
        timeStamp: item.processingTime - earliestTimestamp,
        queueName: item.queueName || queueName
    }));
}