import React from "react";
import { MessageData } from "../../types/messageData";

interface MessageDataTableProps {
    data: MessageData[];
}

export default function messageDataTable({ data }: MessageDataTableProps) {
    const displayData = data.slice(0, 10);

    return (
        <div>
            <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                        <tr>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ID</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Timestamp</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Delay (ms)</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Queue</th>
                        </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                        {displayData.map((message) => (
                            <tr key={message.id}>
                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{message.id}</td>
                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                    {new Date(message.timeStamp * 1000).toLocaleString()}
                                </td>
                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{message.delay.toFixed(2)}</td>
                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{message.queueName}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
            {data.length > 10 && (
                <div className="mt-4 text-sm text-gray-500">
                    Showing 10 of {data.length} entries
                </div>
            )}
        </div>
    );
}