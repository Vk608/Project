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
  cssClass: string;
  icon: string;
}

/**
 * Match type configurations for UI display.
 * Uses CSS classes from the global design system instead of hardcoded colors.
 */
export const MATCH_TYPE_CONFIG: Record<MatchExtent, MatchTypeInfo> = {
  [MatchExtent.EXACT_MATCH]: {
    type: MatchExtent.EXACT_MATCH,
    label: 'Exact Match',
    cssClass: 'match-exact',
    icon: 'check_circle'
  },
  [MatchExtent.MINOR_CHANGE]: {
    type: MatchExtent.MINOR_CHANGE,
    label: 'Minor Change',
    cssClass: 'match-minor',
    icon: 'warning'
  },
  [MatchExtent.MAJOR_DISCREPANCY]: {
    type: MatchExtent.MAJOR_DISCREPANCY,
    label: 'Major Discrepancy',
    cssClass: 'match-major',
    icon: 'error'
  },
  [MatchExtent.NO_MATCH]: {
    type: MatchExtent.NO_MATCH,
    label: 'No Match',
    cssClass: 'match-none',
    icon: 'close'
  }
};
