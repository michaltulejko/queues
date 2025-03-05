import { MessageData } from "../types/messageData";
import { ChartData, DelayData } from "../types/chartData";

export function processChartData(messageData: MessageData[]): ChartData[] {
    // Group data by timestamp and queue name
    const dataByQueue: Record<string, MessageData[]> = {};

    // Group messages by queue
    messageData.forEach(item => {
        if (!dataByQueue[item.queueName]) {
            dataByQueue[item.queueName] = [];
        }
        dataByQueue[item.queueName].push(item);
    });

    // Create timelines for each queue
    const queueTimelines: Record<string, Record<number, number>> = {};

    // For each queue, count messages at each timestamp
    Object.entries(dataByQueue).forEach(([queueName, messages]) => {
        queueTimelines[queueName] = {};

        messages.forEach(msg => {
            // Round to nearest second for better grouping
            const timeInSeconds = Math.round(msg.timeStamp);
            if (!queueTimelines[queueName][timeInSeconds]) {
                queueTimelines[queueName][timeInSeconds] = 0;
            }
            queueTimelines[queueName][timeInSeconds] += 1;
        });
    });

    // Find the max time across all queues to ensure the chart extends fully
    let maxTime = 0;
    Object.values(queueTimelines).forEach(timeline => {
        const times = Object.keys(timeline).map(t => parseInt(t, 10));
        if (times.length > 0) {
            maxTime = Math.max(maxTime, Math.max(...times));
        }
    });

    // Create unified timeline with all queues
    const chartData: ChartData[] = [];

    // Create a data point for each second from 0 to maxTime
    for (let i = 0; i <= maxTime; i++) {
        const dataPoint: any = { timestamp: `${i}s` };

        // Add count for each queue at this timestamp
        Object.keys(queueTimelines).forEach(queueName => {
            dataPoint[queueName] = queueTimelines[queueName][i] || 0;
        });

        chartData.push(dataPoint);
    }

    return chartData;
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