import React, { useMemo } from "react";
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from "recharts";
import { ChartData } from "../../types/chartData";

interface QueueCreationDelayChartProps {
    data: ChartData[];
}

export default function QueueCreationDelayChart({ data }: QueueCreationDelayChartProps) {
    // Check if data is valid for rendering
    if (!data || data.length === 0) {
        return (
            <div className="h-80 flex items-center justify-center bg-gray-50 rounded-lg">
                <div className="text-gray-500">No data available to display</div>
            </div>
        );
    }
    
    // Check if data has required format
    const hasRequiredFormat = data.some(point => 
        typeof point.timestamp === 'string' && 
        (typeof point.Kafka === 'number' || 
         typeof point.SQS === 'number' || 
         typeof point.Rabbit === 'number')
    );
    
    if (!hasRequiredFormat) {
        console.error("QueueCreationDelayChart data missing required format:", data);
        return (
            <div className="h-80 flex items-center justify-center bg-gray-50 rounded-lg">
                <div className="text-red-500">Data format error</div>
            </div>
        );
    }
    
    // Only show every Nth tick if there are lots of data points
    const tickInterval = useMemo(() => {
        if (data.length > 60) return Math.ceil(data.length / 30);
        return 1;
    }, [data.length]);

    return (
        <div className="h-80">
            <ResponsiveContainer width="100%" height="100%">
                <LineChart data={data}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                    <XAxis
                        dataKey="timestamp"
                        tick={{ fill: '#4b5563' }}
                        axisLine={{ stroke: '#9ca3af' }}
                        label={{ value: 'Queue Creation Delay (seconds)', position: 'insideBottomRight', offset: -5 }}
                        tickFormatter={(value, index) => index % tickInterval === 0 ? value : ''}
                    />
                    <YAxis
                        tick={{ fill: '#4b5563' }}
                        axisLine={{ stroke: '#9ca3af' }}
                        label={{ value: 'Message Count', angle: -90, position: 'insideLeft' }}
                    />
                    <Tooltip 
                        contentStyle={{ backgroundColor: 'white', borderColor: '#e5e7eb' }}
                        formatter={(value: any) => [`${value} messages`, undefined]}
                        labelFormatter={(label) => `Delay: ${label}s`}
                        itemSorter={(item) => -(item.value ?? 0)}
                    />
                    <Legend wrapperStyle={{ color: '#4b5563' }} />
                    <Line 
                        type="monotone" 
                        dataKey="Kafka" 
                        stroke="#8884d8" 
                        strokeWidth={2} 
                        dot={data.length > 100 ? false : { r: 2 }}
                        activeDot={{ r: 6 }}
                        isAnimationActive={data.length < 300}
                    />
                    <Line 
                        type="monotone" 
                        dataKey="SQS" 
                        stroke="#82ca9d" 
                        strokeWidth={2}
                        dot={data.length > 100 ? false : { r: 2 }}
                        activeDot={{ r: 6 }}
                        isAnimationActive={data.length < 300}
                    />
                    <Line 
                        type="monotone" 
                        dataKey="Rabbit" 
                        stroke="#ff7300" 
                        strokeWidth={2}
                        dot={data.length > 100 ? false : { r: 2 }}
                        activeDot={{ r: 6 }}
                        isAnimationActive={data.length < 300}
                    />
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}