import React from "react";
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from "recharts";
import { ChartData } from "../../types/chartData";

interface MessageCountChartProps {
    data: ChartData[];
}

// Update the chart to ensure readable text
export default function messageCountChart({ data }: MessageCountChartProps) {
    return (
        <div className="h-80">
            <ResponsiveContainer width="100%" height="100%">
                <LineChart data={data}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                    <XAxis
                        dataKey="timestamp"
                        tick={{ fill: '#4b5563' }} // gray-600 for text
                        axisLine={{ stroke: '#9ca3af' }} // gray-400 for axis
                    />
                    <YAxis
                        tick={{ fill: '#4b5563' }}
                        axisLine={{ stroke: '#9ca3af' }}
                    />
                    <Tooltip contentStyle={{ backgroundColor: 'white', borderColor: '#e5e7eb' }} />
                    <Legend wrapperStyle={{ color: '#4b5563' }} />
                    <Line type="monotone" dataKey="Kafka" stroke="#8884d8" strokeWidth={2} />
                    <Line type="monotone" dataKey="SQS" stroke="#82ca9d" strokeWidth={2} />
                    <Line type="monotone" dataKey="RabbitMQ" stroke="#ff7300" strokeWidth={2} />
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}