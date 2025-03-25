import { BehaviorSubject, Observable, catchError, map, shareReplay, tap, of } from 'rxjs';
import { MessageData } from '../types/messageData';
import { ChartData, DelayData } from '../types/chartData';
import { fetchMessageData } from '../services/queueApi';
import { 
    processChartData, 
    processDelayData, 
    processCreationChartData,
    processCreationDelayChartData
} from '../utils/dataProcessing';

// State interface
interface DataState {
    messageData: MessageData[];
    loading: boolean;
    error: string | null;
    recordsPerQueue: number;
    lastUpdated: number; // Add timestamp to track updates
}

// Initial state
const initialState: DataState = {
    messageData: [],
    loading: false, // Start as false
    error: null,
    recordsPerQueue: 10,
    lastUpdated: 0
};

// Create behavior subject with initial state
const dataSubject = new BehaviorSubject<DataState>(initialState);

// Helper to update state in one place
const updateState = (partialState: Partial<DataState>) => {
    const currentState = dataSubject.getValue();
    const newState = {
        ...currentState,
        ...partialState,
        lastUpdated: Date.now() // Update timestamp
    };
    dataSubject.next(newState);
};

// Selectors - using distinctUntilChanged to prevent unnecessary emissions
export const selectMessageData = (): Observable<MessageData[]> =>
    dataSubject.pipe(
        map(state => state.messageData),
        map(data => [...data]) // Return a new array to avoid reference issues
    );

export const selectLoading = (): Observable<boolean> =>
    dataSubject.pipe(map(state => state.loading));

export const selectError = (): Observable<string | null> =>
    dataSubject.pipe(map(state => state.error));

export const selectRecordsPerQueue = (): Observable<number> =>
    dataSubject.pipe(map(state => state.recordsPerQueue));

export const selectChartData = (): Observable<ChartData[]> =>
    selectMessageData().pipe(
        map(data => {
            console.log("Raw message data length:", data.length);
            if (data.length === 0) return [];
            
            const processedData = processChartData(data);
            console.log("Processed chart data:", processedData);
            return processedData;
        })
    );

export const selectDelayData = (): Observable<DelayData[]> =>
    selectMessageData().pipe(
        map(data => data.length > 0 ? processDelayData(data) : [])
    );

export const selectCreationChartData = (): Observable<ChartData[]> =>
    selectMessageData().pipe(
        map(data => data.length > 0 ? processCreationChartData(data) : [])
    );

export const selectCreationDelayChartData = (): Observable<ChartData[]> =>
    selectMessageData().pipe(
        map(data => data.length > 0 ? processCreationDelayChartData(data) : [])
    );

// Track if a request is in progress
let isLoading = false;

// Actions
export function loadMessageData(): void {
    // Prevent multiple simultaneous requests
    if (isLoading) return;

    const currentState = dataSubject.getValue();
    const recordsAmount = currentState.recordsPerQueue;

    // Set loading state
    updateState({ loading: true, error: null });
    isLoading = true;

    // Subscribe to data observable
    fetchMessageData(recordsAmount)
        .pipe(
            tap(data => {
                updateState({
                    messageData: data,
                    loading: false
                });
                isLoading = false;
            }),
            catchError(err => {
                console.error("Error loading data:", err);
                updateState({
                    messageData: [], // Clear data on error
                    loading: false,
                    error: 'Failed to fetch data: ' + (err.message || 'Unknown error')
                });
                isLoading = false;
                return of([] as MessageData[]); // Return empty array instead of re-throwing
            })
        )
        .subscribe();
}

export function setRecordsAmount(amount: number): void {
    if (amount <= 0) return;

    const currentAmount = dataSubject.getValue().recordsPerQueue;
    if (amount === currentAmount) return; // Don't update if unchanged

    // Update the records amount
    updateState({ recordsPerQueue: amount });

    // Load data with new amount
    loadMessageData();
}