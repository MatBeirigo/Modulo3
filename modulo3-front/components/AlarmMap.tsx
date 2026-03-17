'use client';

import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { AggregatedAlarm } from '@/types/api';
import { formatDistanceToNow } from 'date-fns';
import { ptBR } from 'date-fns/locale';

const SEVERITY_COLORS: Record<string, string> = {
  CRITICAL: '#dc2626',
  HIGH: '#ea580c',
  MEDIUM: '#d97706',
  LOW: '#16a34a',
};

const getColor = (severity: string) => SEVERITY_COLORS[severity] ?? '#6b7280';

interface AlarmMapProps {
  alarms: AggregatedAlarm[];
}

export function AlarmMap({ alarms }: AlarmMapProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<L.Map | null>(null);
  const markersRef = useRef<L.LayerGroup | null>(null);
  const initialFitDone = useRef(false);

  // Initialize map once
  useEffect(() => {
    if (!containerRef.current) return;

    if (mapRef.current) {
      mapRef.current.stop();
      mapRef.current.remove();
      mapRef.current = null;
    }

    const map = L.map(containerRef.current, { zoomAnimation: false }).setView([-18.92, -48.27], 12);
    mapRef.current = map;

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    }).addTo(map);

    const markers = L.layerGroup().addTo(map);
    markersRef.current = markers;

    return () => {
      map.stop();
      map.remove();
      mapRef.current = null;
      markersRef.current = null;
      initialFitDone.current = false;
    };
  }, []);

  // Update markers without resetting the map view
  useEffect(() => {
    const map = mapRef.current;
    const markers = markersRef.current;
    if (!map || !markers) return;

    markers.clearLayers();

    for (const alarm of alarms) {
      const color = getColor(alarm.severity);
      const lastStr = formatDistanceToNow(new Date(alarm.lastOccurrence), {
        addSuffix: true,
        locale: ptBR,
      });

      L.circleMarker([alarm.latitude, alarm.longitude], {
        radius: alarm.eventCount > 10 ? 10 : 6,
        color,
        fillColor: color,
        fillOpacity: 0.8,
        weight: 1.5,
      })
        .bindPopup(`
          <div style="min-width:180px;font-size:13px;line-height:1.5">
            <p style="font-weight:600;margin:0 0 4px">${alarm.eventType}</p>
            <p style="color:#666;margin:0 0 2px;font-size:11px">${alarm.location}</p>
            <p style="margin:0 0 2px;font-size:12px">Severidade: <strong>${alarm.severity}</strong></p>
            <p style="margin:0 0 2px;font-size:12px">Ocorrências: <strong>${alarm.eventCount}</strong></p>
            <p style="color:#666;margin:0;font-size:11px">Último: ${lastStr}</p>
          </div>
        `)
        .addTo(markers);
    }

    // Fit bounds only on first load
    if (!initialFitDone.current && alarms.length > 0) {
      const lats = alarms.map(a => a.latitude);
      const lngs = alarms.map(a => a.longitude);
      map.fitBounds(
        L.latLngBounds(
          [Math.min(...lats) - 0.01, Math.min(...lngs) - 0.01],
          [Math.max(...lats) + 0.01, Math.max(...lngs) + 0.01]
        ),
        { padding: [30, 30] }
      );
      initialFitDone.current = true;
    }
  }, [alarms]);

  return (
    <div
      ref={containerRef}
      style={{ height: '420px', width: '100%', borderRadius: '0 0 0.5rem 0.5rem' }}
    />
  );
}
