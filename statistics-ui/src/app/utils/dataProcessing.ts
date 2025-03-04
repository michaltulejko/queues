import { MessageData } from "../types/messageData";
import { ChartData, DelayData } from "../types/chartData";

export function processChartData(messageData: MessageData[]): ChartData[] {
    // Group data by timestamp and queue name
    const groupedData: Record<number, Record<string, number>> = {};
    const queueTypes = new Set<string>();

    messageData.forEach(item => {
        if (!groupedData[item.timeStamp]) {
            groupedData[item.timeStamp] = {};
        }

        if (!groupedData[item.timeStamp][item.queueName]) {
            groupedData[item.timeStamp][item.queueName] = 0;
        }

        groupedData[item.timeStamp][item.queueName] += 1;
        queueTypes.add(item.queueName);
    });

    // Convert to chart format
    return Object.keys(groupedData).map(timestamp => {
        const entry: any = { timestamp: new Date(parseInt(timestamp) * 1000).toLocaleTimeString() };

        queueTypes.forEach(queue => {
            entry[queue] = groupedData[parseInt(timestamp)][queue] || 0;
        });

        return entry;
    }).sort((a, b) => {
        // Sort by timestamp
        return new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime();
    });
}

export function processDelayData(messageData: MessageData[]): DelayData[] {
    const delaysByQueue: Record<string, { total: number, count: number }> = {};

    messageData.forEach(item => {
        if (!delaysByQueue[item.queueName]) {
            delaysByQueue[item.queueName] = { total: 0, count: 0 };
        }

        delaysByQueue[item.queueName].total += item.delay;
        delaysByQueue[item.queueName].count += 1;
    });

    return Object.keys(delaysByQueue).map(queue => ({
        name: queue,
        delay: delaysByQueue[queue].total / delaysByQueue[queue].count
    }));
}