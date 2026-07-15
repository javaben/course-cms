export interface CourseGroup {
  pkid: number;
  description: string;
}

export interface CourseGroupRequest {
  pkid: number;
  description: string;
}

export interface CourseGroupQuery {
  keyword?: string | null;
}
