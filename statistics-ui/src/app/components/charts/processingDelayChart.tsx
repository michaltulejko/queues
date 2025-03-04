import React from "react";
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from "recharts";
import { DelayData } from "../../types/chartData";

interface ProcessingDelayChartProps {
    data: DelayData[];
}

export default function ProcessingDelayChart({ data }: ProcessingDelayChartProps) {
    return (
        <div className="h-80">
            <ResponsiveContainer width="100%" height="100%">
                <LineChart data={data}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" />
                    <YAxis />
                    <Tooltip />
                    <Legend />
                    <Line type="monotone" dataKey="delay" stroke="#8884d8" />
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}