import React from "react";
import Dashboard from "./components/dashboard/dashboard";

export default function Home() {
  return (
    <div className="min-h-screen p-8 bg-gray-50">
      <h1 className="text-3xl font-bold mb-8">Message Queue Performance Dashboard</h1>
      <Dashboard />
    </div>
  );
}