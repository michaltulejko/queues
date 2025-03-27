import { MessageData } from "../types/messageData";
import { ChartData, DelayData } from "../types/chartData";

export function processChartData(messageData: MessageData[]): ChartData[] {
    // Validate input data first
    if (!messageData || messageData.length === 0) {
        console.warn("No message data to process");
        return [];
    }
    
    // Log a sample message to debug
    console.log("Sample message:", messageData[0]);
    
    // Group data by timestamp and queue name
    const dataByQueue: Record<string, MessageData[]> = {};

    // Group messages by queue
    messageData.forEach(item => {
        if (!dataByQueue[item.queueName]) {
            dataByQueue[item.queueName] = [];
        }
        dataByQueue[item.queueName].push(item);
    });

    // Log queue distribution
    console.log("Messages per queue:", Object.keys(dataByQueue).map(q => 
        `${q}: ${dataByQueue[q].length}`
    ));
    
    // Create timelines for each queue
    const queueTimelines: Record<string, Record<number, number>> = {};
    
    // Track minimum timestamp for each queue independently
    const queueMinTimes: Record<string, number> = {};

    // First pass: find minimum time for each queue
    Object.entries(dataByQueue).forEach(([queueName, messages]) => {
        if (messages.length > 0) {
            const times = messages.map(msg => Math.round(msg.processingTime));
            queueMinTimes[queueName] = Math.min(...times);
        }
    });

    // Second pass: normalize timestamps relative to each queue's minimum time
    Object.entries(dataByQueue).forEach(([queueName, messages]) => {
        queueTimelines[queueName] = {};
        const minTimeForQueue = queueMinTimes[queueName] || 0;

        messages.forEach(msg => {
            // Normalize timestamp to start from 0 for each queue
            const normalizedTime = Math.round(msg.processingTime) - minTimeForQueue;
            
            if (!queueTimelines[queueName][normalizedTime]) {
                queueTimelines[queueName][normalizedTime] = 0;
            }
            queueTimelines[queueName][normalizedTime] += 1;
        });
    });

    // Find the maximum normalized time across all queues
    let maxTime = 0;
    
    Object.values(queueTimelines).forEach(timeline => {
        const times = Object.keys(timeline).map(t => parseInt(t, 10));
        if (times.length > 0) {
            maxTime = Math.max(maxTime, Math.max(...times));
        }
    });
    
    // If no data, return empty array
    if (maxTime === 0) return [];
    
    console.log(`Max normalized time: ${maxTime} seconds`);
    
    // Determine appropriate sampling interval based on time range
    let samplingInterval = 1; // Default: 1 second
    
    // Scale the sampling interval based on the time range
    if (maxTime > 86400) { // > 1 day
        samplingInterval = 3600; // 1 hour
    } else if (maxTime > 3600) { // > 1 hour
        samplingInterval = 60; // 1 minute
    } else if (maxTime > 600) { // > 10 minutes
        samplingInterval = 10; // 10 seconds
    }
    
    console.log(`Using sampling interval: ${samplingInterval} seconds`);
    
    // Calculate how many data points we'll create
    const expectedPoints = Math.ceil(maxTime / samplingInterval) + 1;
    console.log(`Expected data points: ${expectedPoints}`);
    
    // Safety check - if still too many points, increase interval
    if (expectedPoints > 1000) {
        samplingInterval = Math.ceil(maxTime / 1000);
        console.log(`Adjusted sampling interval to: ${samplingInterval} seconds`);
    }
    
    // Create unified timeline with all queues
    const chartData: ChartData[] = [];

    // Create a data point at regular intervals, starting from 0
    for (let i = 0; i <= maxTime; i += samplingInterval) {
        const dataPoint: any = { 
            timestamp: `${i}s` // Time starts from 0 for all queues
        };

        // For each queue, aggregate data within the interval
        Object.keys(queueTimelines).forEach(queueName => {
            let total = 0;
            // Sum all data points in this interval
            for (let j = i; j < i + samplingInterval && j <= maxTime; j++) {
                total += queueTimelines[queueName][j] || 0;
            }
            dataPoint[queueName] = total;
        });

        chartData.push(dataPoint);
        
        // Safety check - if we've created too many points, break
        if (chartData.length > 2000) {
            console.warn('Chart data truncated to prevent memory issues');
            break;
        }
    }

    console.log(`Actual data points created: ${chartData.length}`);
    return chartData;
}

export function processDelayData(messageData: MessageData[]): DelayData[] {
    const delaysByQueue: Record<string, number[]> = {};

    // Group delays by queue
    messageData.forEach(item => {
        if (!delaysByQueue[item.queueName]) {
            delaysByQueue[item.queueName] = [];
        }
        
        // Store individual delays in seconds
        delaysByQueue[item.queueName].push(item.processingDelay / 1000);
    });

    // Calculate proper median for each queue
    return Object.entries(delaysByQueue).map(([queueName, delays]) => {
        // Find median rather than mean for more robust measurement
        const sortedDelays = [...delays].sort((a, b) => a - b);
        let median;
        
        if (sortedDelays.length === 0) {
            median = 0;
        } else if (sortedDelays.length % 2 === 0) {
            const mid = sortedDelays.length / 2;
            median = (sortedDelays[mid - 1] + sortedDelays[mid]) / 2;
        } else {
            median = sortedDelays[Math.floor(sortedDelays.length / 2)];
        }
        
        // Also calculate mean for comparison
        const mean = delays.length > 0 
            ? delays.reduce((sum, delay) => sum + delay, 0) / delays.length 
            : 0;
        
        // Return both metrics
        return {
            name: queueName,
            delay: median, // Use median as primary metric
            meanDelay: mean // Also include mean for comparison
        };
    });
}

export function processCreationDelayChartData(messageData: MessageData[]): ChartData[] {
    // Validate input data
    if (!messageData || messageData.length === 0) {
        console.warn("No message data to process for creation delay chart");
        return [];
    }
    
    // Group data by queue name
    const dataByQueue: Record<string, MessageData[]> = {};

    // Group messages by queue
    messageData.forEach(item => {
        if (!dataByQueue[item.queueName]) {
            dataByQueue[item.queueName] = [];
        }
        dataByQueue[item.queueName].push(item);
    });

    // Create histograms of delays for each queue
    const queueDelayHistograms: Record<string, Record<number, number>> = {};
    
    // Find min and max delay across all queues
    let minDelay = Number.MAX_SAFE_INTEGER;
    let maxDelay = 0;
    
    Object.entries(dataByQueue).forEach(([queueName, messages]) => {
        queueDelayHistograms[queueName] = {};
        
        messages.forEach(msg => {
            // Convert ms to seconds and round to 2 decimal places
            const delaySeconds = Math.round((msg.queueCreationDelay / 1000) * 100) / 100;
            
            // Update min/max
            minDelay = Math.min(minDelay, delaySeconds);
            maxDelay = Math.max(maxDelay, delaySeconds);
            
            // Add to histogram
            if (!queueDelayHistograms[queueName][delaySeconds]) {
                queueDelayHistograms[queueName][delaySeconds] = 0;
            }
            queueDelayHistograms[queueName][delaySeconds]++;
        });
    });
    
    // If no data found
    if (minDelay === Number.MAX_SAFE_INTEGER || maxDelay === 0) {
        console.warn("No valid creation delay data found");
        return [];
    }
    
    console.log(`Creation delay range: ${minDelay}s to ${maxDelay}s`);
    
    // Calculate appropriate bin size based on delay range
    const delayRange = maxDelay - minDelay;
    
    // Determine bin size based on range (in seconds)
    let binSize = 0.01; // Default 0.01s (10ms) bins
    
    if (delayRange > 10) { // > 10 seconds
        binSize = 0.1; // 0.1s bins
    } else if (delayRange > 1) { // > 1 second
        binSize = 0.02; // 0.02s bins
    }
    
    // Make sure we don't create too many bins
    const estimatedBins = Math.ceil(delayRange / binSize);
    if (estimatedBins > 100) {
        binSize = delayRange / 100; // Limit to ~100 bins
    }
    
    console.log(`Using creation bin size: ${binSize}s`);
    
    // Create chart data points
    const chartData: ChartData[] = [];
    
    // Create data points for each delay bin
    for (let delay = minDelay; delay <= maxDelay; delay += binSize) {
        // Round to avoid floating-point precision issues
        const roundedDelay = Math.round(delay * 100) / 100;
        
        const dataPoint: any = {
            // Use delay as the "timestamp" with seconds units
            timestamp: `${roundedDelay}`
        };
        
        // Count messages in this delay bin for each queue
        Object.keys(queueDelayHistograms).forEach(queueName => {
            let count = 0;
            
            // Sum all counts within this bin
            for (
                let d = roundedDelay; 
                d < roundedDelay + binSize && d <= maxDelay; 
                d = Math.round((d + 0.01) * 100) / 100
            ) {
                count += queueDelayHistograms[queueName][d] || 0;
            }
            
            dataPoint[queueName] = count;
        });
        
        chartData.push(dataPoint);
        
        // Safety check - if we've created too many points, break
        if (chartData.length > 1000) {
            console.warn('Creation delay chart - Data truncated to prevent memory issues');
            break;
        }
    }
    
    console.log(`Created ${chartData.length} creation delay distribution data points`);
    return chartData;
}

export function processCreationChartData(messageData: MessageData[]): ChartData[] {
    // Validate input data
    if (!messageData || messageData.length === 0) {
        console.warn("No message data to process for creation chart");
        return [];
    }
    
    // Group data by queue name
    const dataByQueue: Record<string, MessageData[]> = {};

    // Group messages by queue
    messageData.forEach(item => {
        if (!dataByQueue[item.queueName]) {
            dataByQueue[item.queueName] = [];
        }
        dataByQueue[item.queueName].push(item);
    });

    // Create histogram of processing delays
    const queueProcessingDelayHistograms: Record<string, Record<number, number>> = {};
    
    // Find min and max processing delay across all queues
    let minDelay = Number.MAX_SAFE_INTEGER;
    let maxDelay = 0;
    
    Object.entries(dataByQueue).forEach(([queueName, messages]) => {
        queueProcessingDelayHistograms[queueName] = {};
        
        messages.forEach(msg => {
            // Convert ms to seconds and round to 2 decimal places
            const delaySeconds = Math.round((msg.processingDelay / 1000) * 100) / 100;
            
            // Update min/max
            minDelay = Math.min(minDelay, delaySeconds);
            maxDelay = Math.max(maxDelay, delaySeconds);
            
            // Add to histogram
            if (!queueProcessingDelayHistograms[queueName][delaySeconds]) {
                queueProcessingDelayHistograms[queueName][delaySeconds] = 0;
            }
            queueProcessingDelayHistograms[queueName][delaySeconds]++;
        });
    });
    
    // If no data found
    if (minDelay === Number.MAX_SAFE_INTEGER || maxDelay === 0) {
        console.warn("No valid processing delay data found");
        return [];
    }
    
    console.log(`Processing delay range: ${minDelay}s to ${maxDelay}s`);
    
    // Calculate appropriate bin size based on delay range
    const delayRange = maxDelay - minDelay;
    
    // Determine bin size based on range (in seconds)
    let binSize = 0.01; // Default 0.01s (10ms) bins
    
    if (delayRange > 10) { // > 10 seconds
        binSize = 0.1; // 0.1s bins
    } else if (delayRange > 1) { // > 1 second
        binSize = 0.02; // 0.02s bins
    }
    
    // Make sure we don't create too many bins
    const estimatedBins = Math.ceil(delayRange / binSize);
    if (estimatedBins > 100) {
        binSize = delayRange / 100; // Limit to ~100 bins
    }
    
    console.log(`Using processing bin size: ${binSize}s`);
    
    // Create chart data points
    const chartData: ChartData[] = [];
    
    // Create data points for each delay bin
    for (let delay = minDelay; delay <= maxDelay; delay += binSize) {
        // Round to avoid floating-point precision issues
        const roundedDelay = Math.round(delay * 100) / 100;
        
        const dataPoint: any = {
            // Use delay as the "timestamp" with seconds units
            timestamp: `${roundedDelay}`
        };
        
        // Count messages in this delay bin for each queue
        Object.keys(queueProcessingDelayHistograms).forEach(queueName => {
            let count = 0;
            
            // Sum all counts within this bin
            for (
                let d = roundedDelay; 
                d < roundedDelay + binSize && d <= maxDelay; 
                d = Math.round((d + 0.01) * 100) / 100
            ) {
                count += queueProcessingDelayHistograms[queueName][d] || 0;
            }
            
            dataPoint[queueName] = count;
        });
        
        chartData.push(dataPoint);
        
        // Safety check - if we've created too many points, break
        if (chartData.length > 1000) {
            console.warn('Processing delay chart - Data truncated to prevent memory issues');
            break;
        }
    }
    
    console.log(`Created ${chartData.length} processing delay distribution data points`);
    return chartData;
}