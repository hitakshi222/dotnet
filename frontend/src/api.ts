const configuredApiUrl = import.meta.env.VITE_API_URL?.trim() || 'http://localhost:5000';
const apiHostname = configuredApiUrl.replace(/\/+$/, '');
const API_URL = /^https?:\/\//i.test(configuredApiUrl)
  ? apiHostname
  : `https://${apiHostname.includes('.') ? apiHostname : `${apiHostname}.onrender.com`}`;

async function apiFetch(input: RequestInfo | URL, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers);
  headers.set('X-University-Id', localStorage.getItem('campusdesk-university-id') || '1');
  return fetch(input, { ...init, headers });
}

export interface University {
  id: number;
  name: string;
  country: string;
  state: string;
  district: string;
  city: string;
  contactEmail: string;
}

export type UniversityInput = Omit<University, 'id'>;

export async function fetchUniversities(): Promise<University[]> {
  const response = await apiFetch(`${API_URL}/api/universities`);
  await checkResponse(response);
  return response.json();
}

export async function createUniversity(input: UniversityInput): Promise<University> {
  const response = await apiFetch(`${API_URL}/api/universities`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });
  await checkResponse(response);
  return response.json();
}

export interface Teacher {
  id: number;
  fullName: string;
}

export interface Lookup {
  id: number;
  name: string;
}

export interface Period {
  id: number;
  label: string;
  startsAt: string;
  endsAt: string;
}

export interface Room {
  id: number;
  floorNumber: number;
  roomNumber: number;
  name: string;
}

export interface LectureInput {
  teacherId: number;
  classId: number;
  subjectId: number;
  periodId: number;
  roomId: number;
  days: number[];
}

export interface Lecture {
  id: number;
  teacherId: number;
  teacherName: string;
  classId: number;
  className: string;
  subjectId: number;
  subjectName: string;
  dayOfWeek: number;
  periodId: number;
  periodLabel: string;
  roomId: number | null;
  roomName: string | null;
}

export interface Clash {
  dayOfWeek: number;
  rule: string;
  message: string;
  conflictingLectureId: number;
}

export interface ApiError {
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
  clashes?: Clash[];
}

async function checkResponse(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = await response.json().catch(() => ({}));
  throw {
    status: response.status,
    detail: problem.detail || problem.title || `Request failed (${response.status})`,
    errors: problem.errors,
    clashes: problem.clashes,
  } satisfies ApiError;
}

export async function fetchTeachers(): Promise<Teacher[]> {
  const response = await apiFetch(`${API_URL}/api/teachers`);
  await checkResponse(response);
  return response.json();
}

export async function fetchClasses(): Promise<Lookup[]> {
  const response = await apiFetch(`${API_URL}/api/classes`);
  await checkResponse(response);
  return response.json();
}

export async function fetchSubjects(): Promise<Lookup[]> {
  const response = await apiFetch(`${API_URL}/api/subjects`);
  await checkResponse(response);
  return response.json();
}

export async function fetchPeriods(): Promise<Period[]> {
  const response = await apiFetch(`${API_URL}/api/periods`);
  await checkResponse(response);
  return response.json();
}

export async function fetchRooms(): Promise<Room[]> {
  const response = await apiFetch(`${API_URL}/api/rooms`);
  await checkResponse(response);
  return response.json();
}

export async function addRoom(floorNumber: number, roomNumber: number): Promise<Room> {
  const response = await apiFetch(`${API_URL}/api/rooms`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ floorNumber, roomNumber }),
  });
  await checkResponse(response);
  return response.json();
}

export async function saveLectures(input: LectureInput): Promise<Lecture[]> {
  const response = await apiFetch(`${API_URL}/api/lectures`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });

  await checkResponse(response);

  return response.json();
}

export async function fetchTimetable(classId?: number, teacherId?: number): Promise<Lecture[]> {
  const params = new URLSearchParams();
  if (classId) params.append('classId', classId.toString());
  if (teacherId) params.append('teacherId', teacherId.toString());

  const response = await apiFetch(`${API_URL}/api/timetable?${params}`);
  await checkResponse(response);
  return response.json();
}

export async function addTeacher(fullName: string): Promise<Teacher> {
  const response = await apiFetch(`${API_URL}/api/teachers`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ fullName }),
  });

  await checkResponse(response);
  return response.json();
}

export async function addClass(name: string): Promise<Lookup> {
  const response = await apiFetch(`${API_URL}/api/classes`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  });

  await checkResponse(response);
  return response.json();
}

export async function addSubject(name: string): Promise<Lookup> {
  const response = await apiFetch(`${API_URL}/api/subjects`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  });

  await checkResponse(response);
  return response.json();
}

export async function addPeriod(label: string, startsAt: string, endsAt: string): Promise<Period> {
  const response = await apiFetch(`${API_URL}/api/periods`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ label, startsAt, endsAt }),
  });

  await checkResponse(response);
  return response.json();
}

async function update<T>(path: string, value: unknown): Promise<T> {
  const response = await apiFetch(`${API_URL}${path}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(value),
  });
  await checkResponse(response);
  return response.json();
}

async function remove(path: string): Promise<void> {
  const response = await apiFetch(`${API_URL}${path}`, { method: 'DELETE' });
  await checkResponse(response);
}

export const updateTeacher = (id: number, fullName: string) =>
  update<Teacher>(`/api/teachers/${id}`, { fullName });
export const deleteTeacher = (id: number) => remove(`/api/teachers/${id}`);
export const updateClass = (id: number, name: string) =>
  update<Lookup>(`/api/classes/${id}`, { name });
export const deleteClass = (id: number) => remove(`/api/classes/${id}`);
export const updateSubject = (id: number, name: string) =>
  update<Lookup>(`/api/subjects/${id}`, { name });
export const deleteSubject = (id: number) => remove(`/api/subjects/${id}`);
export const updatePeriod = (id: number, label: string, startsAt: string, endsAt: string) =>
  update<Period>(`/api/periods/${id}`, { label, startsAt, endsAt });
export const deletePeriod = (id: number) => remove(`/api/periods/${id}`);
export const updateRoom = (id: number, floorNumber: number, roomNumber: number) =>
  update<Room>(`/api/rooms/${id}`, { floorNumber, roomNumber });
export const deleteRoom = (id: number) => remove(`/api/rooms/${id}`);
export const updateLecture = (id: number, input: LectureInput) =>
  update<Lecture>(`/api/lectures/${id}`, input);
export const deleteLecture = (id: number) => remove(`/api/lectures/${id}`);
