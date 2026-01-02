import { useCallback, useRef } from 'react';
import { Download, FileImage, FileText, FileJson } from 'lucide-react';
import html2canvas from 'html2canvas';
import { jsPDF } from 'jspdf';
import { useSession } from '@/context/SessionContext';

export function ExportButtons() {
  const { sessionSummary, selectedSession } = useSession();
  const dashboardRef = useRef<HTMLElement | null>(null);

  // Get reference to dashboard content on mount
  const setDashboardRef = useCallback(() => {
    dashboardRef.current = document.querySelector('main');
  }, []);

  const exportToPNG = useCallback(async () => {
    setDashboardRef();
    if (!dashboardRef.current) return;

    try {
      const canvas = await html2canvas(dashboardRef.current, {
        backgroundColor: '#ffffff',
        scale: 2,
      });
      const link = document.createElement('a');
      link.download = `combat-report-${Date.now()}.png`;
      link.href = canvas.toDataURL('image/png');
      link.click();
    } catch (error) {
      console.error('Failed to export PNG:', error);
    }
  }, [setDashboardRef]);

  const exportToPDF = useCallback(async () => {
    setDashboardRef();
    if (!dashboardRef.current) return;

    try {
      const canvas = await html2canvas(dashboardRef.current, {
        backgroundColor: '#ffffff',
        scale: 2,
      });
      const imgData = canvas.toDataURL('image/png');

      const pdf = new jsPDF({
        orientation: 'landscape',
        unit: 'px',
        format: [canvas.width / 2, canvas.height / 2],
      });

      pdf.addImage(imgData, 'PNG', 0, 0, canvas.width / 2, canvas.height / 2);
      pdf.save(`combat-report-${Date.now()}.pdf`);
    } catch (error) {
      console.error('Failed to export PDF:', error);
    }
  }, [setDashboardRef]);

  const exportToJSON = useCallback(() => {
    if (!sessionSummary || !selectedSession) return;

    const exportData = {
      exportedAt: new Date().toISOString(),
      session: {
        id: selectedSession.id,
        startTime: selectedSession.startTime.toISOString(),
        endTime: selectedSession.endTime.toISOString(),
        durationMs: selectedSession.durationMs,
        participantCount: selectedSession.participants.length,
        eventCount: selectedSession.events.length,
      },
      summary: {
        totalDamageDealt: selectedSession.summary.totalDamageDealt,
        totalDamageTaken: selectedSession.summary.totalDamageTaken,
        totalHealingDone: selectedSession.summary.totalHealingDone,
        totalHealingReceived: selectedSession.summary.totalHealingReceived,
        deathCount: selectedSession.summary.deathCount,
        ccEventCount: selectedSession.summary.ccEventCount,
      },
      damageMeter: sessionSummary.damageMeter.map((e) => ({
        entity: e.entity.name,
        totalDamage: e.totalDamage,
        dps: e.dps,
        percentage: e.percentage,
        rank: e.rank,
      })),
      healingMeter: sessionSummary.healingMeter.map((e) => ({
        entity: e.entity.name,
        totalHealing: e.totalHealing,
        effectiveHealing: e.effectiveHealing,
        hps: e.hps,
        overhealRate: e.overhealRate,
        rank: e.rank,
      })),
    };

    const blob = new Blob([JSON.stringify(exportData, null, 2)], {
      type: 'application/json',
    });
    const link = document.createElement('a');
    link.download = `combat-data-${Date.now()}.json`;
    link.href = URL.createObjectURL(blob);
    link.click();
    URL.revokeObjectURL(link.href);
  }, [sessionSummary, selectedSession]);

  if (!sessionSummary) {
    return null;
  }

  return (
    <div className="card">
      <h2 className="mb-3 text-sm font-semibold text-slate-700 dark:text-slate-200">
        Export
      </h2>
      <div className="flex flex-col gap-2">
        <button
          onClick={exportToPNG}
          className="btn-secondary flex items-center gap-2"
        >
          <FileImage className="h-4 w-4" />
          <span>Export PNG</span>
        </button>
        <button
          onClick={exportToPDF}
          className="btn-secondary flex items-center gap-2"
        >
          <FileText className="h-4 w-4" />
          <span>Export PDF</span>
        </button>
        <button
          onClick={exportToJSON}
          className="btn-secondary flex items-center gap-2"
        >
          <FileJson className="h-4 w-4" />
          <span>Export JSON</span>
        </button>
      </div>
    </div>
  );
}
