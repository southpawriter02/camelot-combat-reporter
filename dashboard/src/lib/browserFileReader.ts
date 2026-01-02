import { LogParser, CombatEvent } from 'camelot-combat-reporter';

export interface BrowserParsedLog {
  filename: string;
  parseStartTime: Date;
  parseEndTime: Date;
  totalLines: number;
  parsedLines: number;
  events: CombatEvent[];
}

/**
 * Reads a File object in the browser and parses it using LogParser.parseLines()
 */
export async function parseLogFile(file: File): Promise<BrowserParsedLog> {
  const parseStartTime = new Date();
  const parser = new LogParser();

  // Read file as text
  const content = await file.text();
  const lines = content.split(/\r?\n/).filter((line) => line.trim().length > 0);

  // Parse all lines
  const events = parser.parseLines(lines);

  const parseEndTime = new Date();

  return {
    filename: file.name,
    parseStartTime,
    parseEndTime,
    totalLines: lines.length,
    parsedLines: events.length,
    events,
  };
}

/**
 * Validate that a file appears to be a DAoC combat log
 */
export function isValidLogFile(file: File): boolean {
  // Check file extension (allow .log and .txt)
  const validExtensions = ['.log', '.txt'];
  const hasValidExtension = validExtensions.some((ext) =>
    file.name.toLowerCase().endsWith(ext)
  );

  // Check file size (max 100MB)
  const maxSize = 100 * 1024 * 1024;
  const isValidSize = file.size <= maxSize;

  return hasValidExtension && isValidSize;
}
