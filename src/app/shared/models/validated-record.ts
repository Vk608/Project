/**
 * Represents a bibliographic record after validation by the AI Agent.
 * This mirrors the backend ValidatedRecord model.
 */
export interface ValidatedRecord {
  inputPMID: string;
  matchedPMID: string;
  confidenceScore: number;
  match_Extent: MatchExtent;
  discrepancies_Logical: string;
  discrepancies_Metadata: string;
  summary: string;
  originalTitle: string;
}

/**
 * Match extent enumeration
 */
export enum MatchExtent {
  EXACT_MATCH = 'EXACT_MATCH',
  MINOR_CHANGE = 'MINOR_CHANGE',
  MAJOR_DISCREPANCY = 'MAJOR_DISCREPANCY',
  NO_MATCH = 'NO_MATCH'
}

/**
 * Helper type for match types with display properties
 */
export interface MatchTypeInfo {
  type: MatchExtent;
  label: string;
  color: string;
  bgColor: string;
  icon: string;
}

/**
 * Match type configurations for UI display
 */
export const MATCH_TYPE_CONFIG: Record<MatchExtent, MatchTypeInfo> = {
  [MatchExtent.EXACT_MATCH]: {
    type: MatchExtent.EXACT_MATCH,
    label: 'Exact Match',
    color: '#2e7d32',
    bgColor: '#e8f5e9',
    icon: 'check_circle'
  },
  [MatchExtent.MINOR_CHANGE]: {
    type: MatchExtent.MINOR_CHANGE,
    label: 'Minor Change',
    color: '#f57f17',
    bgColor: '#fff3e0',
    icon: 'warning'
  },
  [MatchExtent.MAJOR_DISCREPANCY]: {
    type: MatchExtent.MAJOR_DISCREPANCY,
    label: 'Major Discrepancy',
    color: '#c62828',
    bgColor: '#ffebee',
    icon: 'error'
  },
  [MatchExtent.NO_MATCH]: {
    type: MatchExtent.NO_MATCH,
    label: 'No Match',
    color: '#616161',
    bgColor: '#f5f5f5',
    icon: 'close'
  }
};
