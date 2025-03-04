"use client";

import React, { useEffect, useState, useRef } from "react";
import { useObservable } from "../../hooks/useObservable";
import MessageCountChart from "../charts/messageCountChart";
import ProcessingDelayChart from "../charts/processingDelayChart";
import MessageDataTable from "../tables/messageDataTable";
import {
    loadMessageData,
    selectChartData,
    selectDelayData,
    selectLoading,
    selectError,
    selectMessageData,
    selectRecordsPerQueue,
    setRecordsAmount
} from "../../store/dataStore";

export default function Dashboard() {
    // Use refs to prevent renders from changing dependencies
    const initialRender = useRef(true);

    // Subscribe to observables
    const loading = useObservable(selectLoading(), false);
    const error = useObservable(selectError(), null);
    const chartData = useObservable(selectChartData(), []);
    const delayData = useObservable(selectDelayData(), []);
    const messageData = useObservable(selectMessageData(), []);
    const recordsPerQueue = useObservable(selectRecordsPerQueue(), 10);

    // Local state for input field - with string
    const [recordsInput, setRecordsInput] = useState("10");

    // Handle initial data loading
    useEffect(() => {
        if (initialRender.current) {
            initialRender.current = false;
            loadMessageData();
        }
    }, []);

    // Synchronize recordsPerQueue with input, but avoid infinite loop
    const recordsPerQueueRef = useRef(recordsPerQueue);

    useEffect(() => {
        // Only update if value changed and it wasn't from input
        if (recordsPerQueueRef.current !== recordsPerQueue) {
            recordsPerQueueRef.current = recordsPerQueue;
            setRecordsInput(recordsPerQueue.toString());
        }
    }, [recordsPerQueue]);

    const handleRecordsChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setRecordsInput(e.target.value);
    };

    const handleRecordsSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        const amount = parseInt(recordsInput, 10);
        if (!isNaN(amount) && amount > 0) {
            recordsPerQueueRef.current = amount; // Update ref to prevent loop
            setRecordsAmount(amount);
        }
    };

    const handleRefresh = () => {
        loadMessageData();
    };

    return (
        <div className="space-y-8">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 bg-white p-4 rounded-lg shadow-sm border border-gray-100">
                <div>
                    <h2 className="text-lg font-medium text-gray-700">Dashboard Controls</h2>
                </div>
                <div className="flex flex-col sm:flex-row gap-4">
                    <form onSubmit={handleRecordsSubmit} className="flex items-center gap-2">
                        <label htmlFor="recordsAmount" className="text-sm text-gray-600">
                            Records per queue:
                        </label>
                        <input
                            type="number"
                            id="recordsAmount"
                            className="w-20 px-2 py-1 rounded border border-gray-300 text-sm"
                            value={recordsInput}
                            onChange={handleRecordsChange}
                            min="1"
                        />
                        <button
                            type="submit"
                            className="px-3 py-1 bg-blue-500 text-white rounded text-sm hover:bg-blue-600"
                        >
                            Apply
                        </button>
                    </form>
                    <button
                        onClick={handleRefresh}
                        className="px-3 py-1 bg-gray-100 text-gray-800 rounded text-sm hover:bg-gray-200 flex items-center gap-1"
                        disabled={loading}
                    >
                        {loading ? "Loading..." : (
                            <>
                                <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                                </svg>
                                Refresh
                            </>
                        )}
                    </button>
                </div>
            </div>

            {loading && <div className="p-4 text-gray-600 font-medium">Loading data...</div>}
            {error && <div className="p-4 text-red-600 font-medium">Error: {error}</div>}

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                <div className="bg-white p-6 rounded-lg shadow-md border border-gray-100">
                    <h2 className="text-xl font-semibold mb-4 text-gray-800">Message Processing Count</h2>
                    <MessageCountChart data={chartData} />
                </div>

                <div className="bg-white p-6 rounded-lg shadow-md border border-gray-100">
                    <h2 className="text-xl font-semibold mb-4 text-gray-800">Average Processing Delay</h2>
                    <ProcessingDelayChart data={delayData} />
                </div>

                <div className="bg-white p-6 rounded-lg shadow-md border border-gray-100 lg:col-span-2">
                    <h2 className="text-xl font-semibold mb-4 text-gray-800">
                        Message Processing Data
                        <span className="text-sm font-normal text-gray-500 ml-2">
                            ({messageData.length} records)
                        </span>
                    </h2>
                    <MessageDataTable data={messageData} />
                </div>
            </div>
        </div>
    );
}