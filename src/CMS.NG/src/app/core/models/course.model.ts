import { PartnerLookup } from '@core/models/partner-lookup.model';
import { CourseGroupLookup } from '@core/models/course-group-lookup.model';
import { PublishStatusLookup } from '@core/models/publish-status-lookup.model';

export interface Course {
  pkid: number;
  title: string;
  officialTitle?: string | null;
  courseId: string;
  prodCourseId: string;
  friendlyUrl: string;
  displayOrder: number;
  partnerPkid: number;
  courseGroupPkid?: number | null;
  publishStatusPkid: number;
  scheduleOn: string; // ISO date (yyyy-MM-dd)
  scheduleOff: string;
  hour: number;
  listPrice: number;
  learningCredit: number;
  material?: string | null;
  objective?: string | null;
  target?: string | null;
  prerequisites?: string | null;
  outline?: string | null;
  towardCertOrExam?: string | null;
  note?: string | null;
  otherInfo?: string | null;
  canRepeat: boolean;
  // FK nav labels (from the multi-map read):
  partner?: PartnerLookup | null;
  courseGroup?: CourseGroupLookup | null;
  publishStatus?: PublishStatusLookup | null;
  // N-N id lists (populated on get-by-id):
  certificationPkids: number[];
  jobCategoryPkids: number[];
}

export interface CourseRequest {
  pkid: number;
  title: string;
  officialTitle?: string | null;
  courseId: string;
  prodCourseId: string;
  friendlyUrl: string;
  displayOrder: number;
  partnerPkid: number;
  courseGroupPkid?: number | null;
  publishStatusPkid: number;
  scheduleOn: string;
  scheduleOff: string;
  hour: number;
  listPrice: number;
  learningCredit: number;
  material?: string | null;
  objective?: string | null;
  target?: string | null;
  prerequisites?: string | null;
  outline?: string | null;
  towardCertOrExam?: string | null;
  note?: string | null;
  otherInfo?: string | null;
  canRepeat: boolean;
  certificationPkids: number[];
  jobCategoryPkids: number[];
}

export interface CourseQuery {
  keyword?: string | null;
  partnerPkid?: number | null;
  courseGroupPkid?: number | null;
  publishStatusPkid?: number | null;
  scheduleOnFrom?: string | null;
  scheduleOnTo?: string | null;
  scheduleOffFrom?: string | null;
  scheduleOffTo?: string | null;
  canRepeat?: boolean | null;
}
