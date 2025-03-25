import React from "react";
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, LabelList } from "recharts";
import { DelayData } from "../../types/chartData";

interface ProcessingDelayChartProps {
    data: DelayData[];
}

export default function ProcessingDelayChart({ data }: ProcessingDelayChartProps) {
    // Check if data is valid for rendering
    if (!data || data.length === 0) {
        return (
            <div className="h-80 flex items-center justify-center bg-gray-50 rounded-lg">
                <div className="text-gray-500">No data available to display</div>
            </div>
        );
    }
    
    return (
        <div className="h-80">
            <ResponsiveContainer width="100%" height="100%">
                <BarChart data={data} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" />
                    <YAxis 
                        label={{ value: 'Seconds', angle: -90, position: 'insideLeft' }} 
                    />
                    <Tooltip 
                        formatter={(value: number) => [`${value.toFixed(3)}s`, 'Processing Delay']}
                    />
                    <Legend />
                    <Bar 
                        dataKey="delay" 
                        fill="#8884d8" 
                        name="Median Processing Delay"
                    >
                        <LabelList 
                            dataKey="delay" 
                            position="top" 
                            formatter={(value: number) => `${value.toFixed(3)}s`}
                            style={{ fontSize: '12px', fill: '#666' }}
                        />
                    </Bar>
                </BarChart>
            </ResponsiveContainer>
        </div>
    );
}