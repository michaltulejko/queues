export interface MessageData {
    id: string;
    timeStamp: number;
    originalTimeStamp?: number;
    delay: number;
    queueName: string;
}