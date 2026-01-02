import { extractTimestamp, hasValidTimestamp } from '../../../src/utils/timestamp';
import { ParseLineError, ParseErrorReason } from '../../../src/errors';

describe('timestamp utilities', () => {
  describe('hasValidTimestamp', () => {
    it('should return true for valid timestamp', () => {
      expect(hasValidTimestamp('[12:34:56] Some message')).toBe(true);
    });

    it('should return true for midnight', () => {
      expect(hasValidTimestamp('[00:00:00] Some message')).toBe(true);
    });

    it('should return true for end of day', () => {
      expect(hasValidTimestamp('[23:59:59] Some message')).toBe(true);
    });

    it('should return false for missing timestamp', () => {
      expect(hasValidTimestamp('Some message without timestamp')).toBe(false);
    });

    it('should return false for malformed timestamp', () => {
      expect(hasValidTimestamp('[12:34] Some message')).toBe(false);
      expect(hasValidTimestamp('[12:34:5] Some message')).toBe(false);
      expect(hasValidTimestamp('[1:34:56] Some message')).toBe(false);
    });

    it('should return false for timestamp not at start', () => {
      expect(hasValidTimestamp('Message [12:34:56]')).toBe(false);
    });
  });

  describe('extractTimestamp', () => {
    it('should extract valid timestamp', () => {
      const result = extractTimestamp('[12:34:56] Some message', 1);

      expect(result.rawTimestamp).toBe('[12:34:56]');
      expect(result.timestamp.getHours()).toBe(12);
      expect(result.timestamp.getMinutes()).toBe(34);
      expect(result.timestamp.getSeconds()).toBe(56);
      expect(result.message).toBe('Some message');
    });

    it('should use provided base date', () => {
      // Create date explicitly to avoid timezone issues
      const baseDate = new Date(2024, 5, 15); // June 15, 2024 in local timezone
      const result = extractTimestamp('[12:34:56] Some message', 1, baseDate);

      expect(result.timestamp.getFullYear()).toBe(2024);
      expect(result.timestamp.getMonth()).toBe(5); // June is 5 (0-indexed)
      expect(result.timestamp.getDate()).toBe(15);
    });

    it('should throw for missing timestamp', () => {
      expect(() => extractTimestamp('No timestamp here', 1)).toThrow(ParseLineError);
      expect(() => extractTimestamp('No timestamp here', 1)).toThrow(
        expect.objectContaining({
          reason: ParseErrorReason.MISSING_TIMESTAMP,
        })
      );
    });

    it('should throw for invalid hour', () => {
      expect(() => extractTimestamp('[25:00:00] Message', 1)).toThrow(ParseLineError);
      expect(() => extractTimestamp('[25:00:00] Message', 1)).toThrow(
        expect.objectContaining({
          reason: ParseErrorReason.INVALID_TIMESTAMP_FORMAT,
        })
      );
    });

    it('should throw for invalid minute', () => {
      expect(() => extractTimestamp('[12:60:00] Message', 1)).toThrow(ParseLineError);
    });

    it('should throw for invalid second', () => {
      expect(() => extractTimestamp('[12:00:60] Message', 1)).toThrow(ParseLineError);
    });

    it('should handle message with extra spaces', () => {
      const result = extractTimestamp('[12:34:56]   Message with spaces  ', 1);
      expect(result.message).toBe('Message with spaces');
    });

    it('should handle empty message after timestamp', () => {
      const result = extractTimestamp('[12:34:56]', 1);
      expect(result.message).toBe('');
    });
  });
});
