export interface MessageData {
    id: string;
    queueCreationTime: number;
    queueCreationDelay: number;
    processingTime: number;
    processingDelay: number;
    queueName: string;
}