export interface ChartData {
    timestamp: string;
    [key: string]: string | number;
}

export interface DelayData {
    name: string;
    delay: number;
}