import { useState, useEffect, type FormEvent } from 'react';
import {
  fetchTeachers,
  fetchClasses,
  fetchSubjects,
  fetchPeriods,
  fetchRooms,
  saveLectures,
  fetchTimetable,
  addTeacher,
  addClass,
  addSubject,
  addPeriod,
  addRoom,
  updateTeacher,
  updateClass,
  updateSubject,
  updatePeriod,
  updateRoom,
  deleteTeacher,
  deleteClass,
  deleteSubject,
  deletePeriod,
  deleteRoom,
  updateLecture,
  deleteLecture,
  fetchUniversities,
  createUniversity,
} from './api';
import type {
  Teacher,
  Lookup,
  Period,
  Room,
  Lecture,
  Clash,
  ApiError,
  University,
} from './api';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

const DAYS = [
  { value: 1, label: 'Monday', short: 'Mon' },
  { value: 2, label: 'Tuesday', short: 'Tue' },
  { value: 3, label: 'Wednesday', short: 'Wed' },
  { value: 4, label: 'Thursday', short: 'Thu' },
  { value: 5, label: 'Friday', short: 'Fri' },
  { value: 6, label: 'Saturday', short: 'Sat' },
];

function getErrorMessage(error: unknown): string {
  if (isApiError(error)) {
    return String(error.detail || 'The request could not be completed.');
  }
  return error instanceof Error ? error.message : 'The request could not be completed.';
}

function isApiError(error: unknown): error is ApiError {
  return typeof error === 'object' && error !== null && 'status' in error;
}

function App() {
  const [universities, setUniversities] = useState<University[]>([]);
  const [activeUniversity, setActiveUniversity] = useState<University | null>(null);
  const [selectedUniversityId, setSelectedUniversityId] = useState('');
  const [showUniversityRegistration, setShowUniversityRegistration] = useState(false);
  const [universityLoading, setUniversityLoading] = useState(true);
  const [universitySaving, setUniversitySaving] = useState(false);
  const [universityError, setUniversityError] = useState('');
  const [universityForm, setUniversityForm] = useState({ name: '', country: '', state: '', district: '', city: '', contactEmail: '' });
  const [view, setView] = useState<'form' | 'report' | 'manage'>('form');
  const [reportType, setReportType] = useState<'class' | 'teacher'>('class');
  const [manageType, setManageType] = useState<'teacher' | 'class' | 'subject' | 'period' | 'infrastructure'>('teacher');

  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [classes, setClasses] = useState<Lookup[]>([]);
  const [subjects, setSubjects] = useState<Lookup[]>([]);
  const [periods, setPeriods] = useState<Period[]>([]);
  const [rooms, setRooms] = useState<Room[]>([]);

  const [teacherId, setTeacherId] = useState<number>(0);
  const [classId, setClassId] = useState<number>(0);
  const [subjectId, setSubjectId] = useState<number>(0);
  const [periodId, setPeriodId] = useState<number>(0);
  const [roomId, setRoomId] = useState<number>(0);
  const [selectedDays, setSelectedDays] = useState<number[]>([]);

  const [message, setMessage] = useState('');
  const [errors, setErrors] = useState<string[]>([]);
  const [clashes, setClashes] = useState<Clash[]>([]);
  const [isSaving, setIsSaving] = useState(false);

  const [reportClassId, setReportClassId] = useState<number>(0);
  const [reportTeacherId, setReportTeacherId] = useState<number>(0);
  const [timetable, setTimetable] = useState<Lecture[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  // Data management form state
  const [managedName, setManagedName] = useState('');
  const [newPeriodLabel, setNewPeriodLabel] = useState('');
  const [newPeriodStart, setNewPeriodStart] = useState('');
  const [newPeriodEnd, setNewPeriodEnd] = useState('');
  const [newRoomFloor, setNewRoomFloor] = useState('');
  const [newRoomNumber, setNewRoomNumber] = useState('');
  const [editingLookupId, setEditingLookupId] = useState<number | null>(null);
  const [editingLectureId, setEditingLectureId] = useState<number | null>(null);
  const [isAdding, setIsAdding] = useState(false);

  useEffect(() => {
    void initializeUniversityWorkspace();
  }, []);

  async function initializeUniversityWorkspace() {
    setUniversityLoading(true);
    try {
      const list = await fetchUniversities();
      setUniversities(list);
      const savedId = Number(localStorage.getItem('campusdesk-university-id'));
      const savedUniversity = list.find((university) => university.id === savedId);
      if (savedUniversity) {
        setActiveUniversity(savedUniversity);
        await loadLookupData();
      } else {
        localStorage.removeItem('campusdesk-university-id');
      }
    } catch (error) {
      setUniversityError(`Could not connect to the university directory: ${getErrorMessage(error)}`);
    } finally {
      setUniversityLoading(false);
    }
  }

  async function enterUniversity(university: University) {
    localStorage.setItem('campusdesk-university-id', String(university.id));
    setActiveUniversity(university);
    setUniversityError('');
    await loadLookupData();
  }

  async function registerUniversity(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setUniversityError('');
    setUniversitySaving(true);
    try {
      const created = await createUniversity(universityForm);
      const next = [...universities, created].sort((a, b) => a.name.localeCompare(b.name));
      setUniversities(next);
      setShowUniversityRegistration(false);
      setUniversityForm({ name: '', country: '', state: '', district: '', city: '', contactEmail: '' });
      await enterUniversity(created);
    } catch (error) {
      setUniversityError(getErrorMessage(error));
    } finally {
      setUniversitySaving(false);
    }
  }

  function switchUniversity() {
    localStorage.removeItem('campusdesk-university-id');
    setActiveUniversity(null);
    setTimetable([]);
    setMessage('');
    setErrors([]);
  }

  async function loadLookupData() {
    try {
      const [teachersData, classesData, subjectsData, periodsData, roomsData] = await Promise.all([
        fetchTeachers(),
        fetchClasses(),
        fetchSubjects(),
        fetchPeriods(),
        fetchRooms(),
      ]);
      setTeachers(teachersData);
      setClasses(classesData);
      setSubjects(subjectsData);
      setPeriods(periodsData);
      setRooms(roomsData);
    } catch (error) {
      console.error('Failed to load lookup data:', error);
      setMessage('');
      setErrors([`Could not load form data: ${getErrorMessage(error)}`]);
    }
  }

  const handleDayToggle = (day: number) => {
    setSelectedDays((prev) =>
      prev.includes(day) ? prev.filter((d) => d !== day) : [...prev, day]
    );
  };

  const handleSave = async () => {
    setMessage('');
    setErrors([]);
    setClashes([]);
    setIsSaving(true);

    try {
      const input = {
        teacherId,
        classId,
        subjectId,
        periodId,
        roomId,
        days: selectedDays,
      };
      if (editingLectureId !== null) {
        await updateLecture(editingLectureId, input);
        setMessage('Lecture updated successfully.');
        setEditingLectureId(null);
      } else {
        const saved = await saveLectures(input);
        setMessage(`${saved.length} lectures saved successfully!`);
        setSelectedDays([]);
      }
    } catch (error: unknown) {
      if (isApiError(error) && error.status === 400 && error.errors) {
        setErrors(Object.values(error.errors).flat());
      } else if (isApiError(error) && error.status === 409 && error.clashes) {
        setClashes(error.clashes);
      } else {
        setErrors([getErrorMessage(error)]);
      }
    } finally {
      setIsSaving(false);
    }
  };

  const handleClear = () => {
    setEditingLectureId(null);
    setTeacherId(0);
    setClassId(0);
    setSubjectId(0);
    setPeriodId(0);
    setRoomId(0);
    setSelectedDays([]);
    setMessage('');
    setErrors([]);
    setClashes([]);
  };

  const handleLoadTimetable = async () => {
    if (reportType === 'class' && reportClassId <= 0) {
      setTimetable([]);
      setMessage('Choose a class before loading its timetable.');
      return;
    }
    setMessage('');
    setIsLoading(true);
    try {
      const data = await fetchTimetable(
        reportType === 'class' ? reportClassId : undefined,
        reportType === 'teacher' ? reportTeacherId : undefined
      );
      setTimetable(data);
    } catch (error) {
      console.error('Failed to load timetable:', error);
      setMessage(getErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  };

  const handleSaveManagedItem = async () => {
    if (manageType === 'period') {
      if (!newPeriodLabel.trim() || !newPeriodStart || !newPeriodEnd) {
        setMessage('Enter a period label, start time, and end time.');
        return;
      }
      if (newPeriodEnd <= newPeriodStart) {
        setMessage('Period end time must be later than start time.');
        return;
      }
    } else if (manageType === 'infrastructure') {
      const floorNumber = Number(newRoomFloor);
      const roomNumber = Number(newRoomNumber);
      if (newRoomFloor.trim() === '' || newRoomNumber.trim() === '' ||
          !Number.isInteger(floorNumber) || floorNumber < 0 || floorNumber > 2147483647 ||
          !Number.isInteger(roomNumber) || roomNumber <= 0 || roomNumber > 2147483647) {
        setMessage('Enter a floor number of zero or greater and a room number greater than zero.');
        return;
      }
    } else if (!managedName.trim()) {
      setMessage(`${manageType[0].toUpperCase()}${manageType.slice(1)} name is required.`);
      return;
    }
    setIsAdding(true);
    try {
      const id = editingLookupId;
      if (manageType === 'teacher') {
        const value = managedName.trim();
        if (id === null) await addTeacher(value);
        else await updateTeacher(id, value);
      } else if (manageType === 'class') {
        const value = managedName.trim();
        if (id === null) await addClass(value);
        else await updateClass(id, value);
      } else if (manageType === 'subject') {
        const value = managedName.trim();
        if (id === null) await addSubject(value);
        else await updateSubject(id, value);
      } else if (manageType === 'period') {
        if (id === null) await addPeriod(newPeriodLabel.trim(), newPeriodStart, newPeriodEnd);
        else await updatePeriod(id, newPeriodLabel.trim(), newPeriodStart, newPeriodEnd);
      } else if (manageType === 'infrastructure') {
        const floorNumber = Number(newRoomFloor);
        const roomNumber = Number(newRoomNumber);
        if (id === null) await addRoom(floorNumber, roomNumber);
        else await updateRoom(id, floorNumber, roomNumber);
      }
      const itemType = manageType === 'infrastructure' ? 'Infrastructure' : `${manageType[0].toUpperCase()}${manageType.slice(1)}`;
      setMessage(`${itemType} ${id === null ? 'added' : 'updated'} successfully.`);
      setEditingLookupId(null);
      setManagedName('');
      setNewPeriodLabel('');
      setNewPeriodStart('');
      setNewPeriodEnd('');
      setNewRoomFloor('');
      setNewRoomNumber('');
      await loadLookupData();
    } catch (error) {
      setMessage(getErrorMessage(error));
    } finally {
      setIsAdding(false);
    }
  };

  const handleEditManagedItem = (id: number, name: string, period?: Period, room?: Room) => {
    setEditingLookupId(id);
    setManagedName(name);
    setNewPeriodLabel(period?.label || '');
    setNewPeriodStart(period?.startsAt || '');
    setNewPeriodEnd(period?.endsAt || '');
    setNewRoomFloor(room ? String(room.floorNumber) : '');
    setNewRoomNumber(room ? String(room.roomNumber) : '');
    setMessage('');
  };

  const handleDeleteManagedItem = async (id: number, name: string) => {
    if (!window.confirm(`Delete "${name}"? Items used by lectures cannot be deleted.`)) return;
    setIsAdding(true);
    try {
      if (manageType === 'teacher') await deleteTeacher(id);
      else if (manageType === 'class') await deleteClass(id);
      else if (manageType === 'subject') await deleteSubject(id);
      else if (manageType === 'period') await deletePeriod(id);
      else await deleteRoom(id);
      setMessage(`${name} deleted.`);
      await loadLookupData();
    } catch (error) {
      setMessage(getErrorMessage(error));
    } finally {
      setIsAdding(false);
    }
  };

  const handleEditLecture = (lecture: Lecture) => {
    setTeacherId(lecture.teacherId);
    setClassId(lecture.classId);
    setSubjectId(lecture.subjectId);
    setPeriodId(lecture.periodId);
    setRoomId(lecture.roomId ?? 0);
    setSelectedDays([]);
    setEditingLectureId(lecture.id);
    setMessage('Editing this lecture. Its day remains unchanged.');
    setErrors([]);
    setClashes([]);
    setView('form');
  };

  const handleDeleteLecture = async (lecture: Lecture) => {
    if (!window.confirm(`Delete ${lecture.subjectName} for ${lecture.className}?`)) return;
    try {
      await deleteLecture(lecture.id);
      setMessage('Lecture deleted.');
      await handleLoadTimetable();
    } catch (error) {
      setMessage(getErrorMessage(error));
    }
  };

  const getPeriodTime = (period: Period) => `${period.label} · ${period.startsAt}–${period.endsAt}`;

  const renderLectureActions = (lectures: Lecture[]) => (
    <div className="lecture-actions-list">
      {lectures.map((lecture) => {
        const dayName = DAYS.find((day) => day.value === lecture.dayOfWeek)?.label || 'Unknown day';
        return (
          <div className="lecture-action-row" key={lecture.id}>
            <span>
              <strong>{lecture.subjectName}</strong>
              <small>{lecture.className} · {lecture.teacherName} · {dayName} · {lecture.periodLabel} · {lecture.roomName || 'Location not assigned'}</small>
            </span>
            <span className="item-actions">
              <button type="button" className="btn-link" onClick={() => handleEditLecture(lecture)}>Edit</button>
              <button type="button" className="btn-link danger-link" onClick={() => handleDeleteLecture(lecture)}>Delete</button>
            </span>
          </div>
        );
      })}
    </div>
  );

  const renderForm = () => (
    <div className="container">
      <div className="card">
        <div className="card-header">
          <h1>{editingLectureId === null ? 'Lecture Form' : 'Edit Lecture'}</h1>
          <p className="subtitle">Schedule teaching sessions and validate the weekly timetable.</p>
        </div>

        <div className="form-grid">
          <div className="form-group">
            <label htmlFor="lecture-teacher">Teacher</label>
            <select
              id="lecture-teacher"
              value={teacherId}
              onChange={(e) => setTeacherId(Number(e.target.value))}
              className="form-control"
            >
              <option value={0}>Select teacher</option>
              {teachers.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.fullName}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="lecture-class">Class</label>
            <select
              id="lecture-class"
              value={classId}
              onChange={(e) => setClassId(Number(e.target.value))}
              className="form-control"
            >
              <option value={0}>Select class</option>
              {classes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="lecture-subject">Subject</label>
            <select
              id="lecture-subject"
              value={subjectId}
              onChange={(e) => setSubjectId(Number(e.target.value))}
              className="form-control"
            >
              <option value={0}>Select subject</option>
              {subjects.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="lecture-period">Period</label>
            <select
              id="lecture-period"
              value={periodId}
              onChange={(e) => setPeriodId(Number(e.target.value))}
              className="form-control"
            >
              <option value={0}>Select period</option>
              {periods.map((p) => (
                <option key={p.id} value={p.id}>
                  {getPeriodTime(p)}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="lecture-room">Infrastructure / Room</label>
            <select
              id="lecture-room"
              value={roomId}
              onChange={(e) => setRoomId(Number(e.target.value))}
              className="form-control"
              required
            >
              <option value={0}>Select infrastructure</option>
              {rooms.map((room) => (
                <option key={room.id} value={room.id}>{room.name}</option>
              ))}
            </select>
          </div>
        </div>

        {editingLectureId === null ? (
          <div className="form-group">
            <label className="days-label">Select days (choose at least one)</label>
            <div className="checkboxes">
              {DAYS.map((day) => (
                <label key={day.value} className="checkbox-card">
                  <input
                    type="checkbox"
                    checked={selectedDays.includes(day.value)}
                    onChange={() => handleDayToggle(day.value)}
                  />
                  <span className="checkbox-content">
                    <span className="day-short">{day.short}</span>
                    <span className="day-full">{day.label}</span>
                  </span>
                </label>
              ))}
            </div>
          </div>
        ) : (
          <p className="help-note">Editing changes the teacher, class, subject, or period for this lecture. Its day stays the same.</p>
        )}

        {message && <div className="alert success">{message}</div>}
        {errors.length > 0 && (
          <div className="alert error">
            {errors.map((e, i) => (
              <div key={i}>⚠️ {e}</div>
            ))}
          </div>
        )}
        {clashes.length > 0 && (
          <div className="alert error">
            {clashes.map((c, i) => (
              <div key={i}>🚫 {c.message}</div>
            ))}
          </div>
        )}

        <div className="buttons">
          <button onClick={handleSave} className="btn primary" disabled={isSaving || teachers.length === 0 || classes.length === 0 || subjects.length === 0 || periods.length === 0 || rooms.length === 0}>
            {isSaving ? 'Saving…' : editingLectureId === null ? 'Save Lectures' : 'Update Lecture'}
          </button>
          <button onClick={handleClear} className="btn secondary">
            {editingLectureId === null ? 'Clear Form' : 'Cancel Edit'}
          </button>
        </div>
      </div>
    </div>
  );

  const renderReport = () => {
    const sortedPeriods = [...periods].sort((a, b) => a.startsAt.localeCompare(b.startsAt));

    if (reportType === 'teacher' && reportTeacherId === 0) {
      const teacherLectures = teachers.map((teacher) => {
        const teacherData = timetable.filter((l) => l.teacherId === teacher.id);
        return { teacher, lectures: teacherData };
      });

      return (
        <div className="container">
          <div className="card">
            <div className="card-header">
              <h1>📊 Timetable Report</h1>
              <p className="subtitle">View schedules by teacher</p>
            </div>

            <div className="form-group">
              <label htmlFor="report-type-all">View by</label>
              <select
                id="report-type-all"
                value={reportType}
                onChange={(e) => setReportType(e.target.value as 'class' | 'teacher')}
                className="form-control"
              >
                <option value="class">Class</option>
                <option value="teacher">Teacher</option>
              </select>
            </div>

            <div className="teacher-list">
              {teacherLectures.map(({ teacher, lectures }) => {
                const tableId = `teacher-table-${teacher.id}`;
                
                return (
                  <div key={teacher.id} className="teacher-card">
                    <h3>👤 {teacher.fullName}</h3>
                    {lectures.length === 0 ? (
                      <p className="no-data">No lectures scheduled yet</p>
                    ) : (
                      <>
                        {renderGrid(lectures, sortedPeriods, 'teacher', tableId)}
                        <p className="total-badge">{lectures.length} lectures/week</p>
                        {renderLectureActions(lectures)}
                        <button 
                          onClick={() => {
                            const table = document.getElementById(tableId) as HTMLTableElement;
                            exportToPDF(table, `${teacher.fullName}'s Timetable`);
                          }} 
                          className="btn secondary" 
                          style={{ marginTop: '10px' }}
                        >
                          📄 Export PDF
                        </button>
                      </>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      );
    }

    return (
      <div className="container">
        <div className="card">
          <div className="card-header">
            <h1>📊 Timetable Report</h1>
            <p className="subtitle">View schedules by class or teacher</p>
          </div>

          <div className="form-grid">
            <div className="form-group">
              <label htmlFor="report-type">View by</label>
              <select
                id="report-type"
                value={reportType}
                onChange={(e) => setReportType(e.target.value as 'class' | 'teacher')}
                className="form-control"
              >
                <option value="class">Class</option>
                <option value="teacher">Teacher</option>
              </select>
            </div>

            {reportType === 'class' && (
              <div className="form-group">
                <label htmlFor="report-class">Class</label>
                <select
                  id="report-class"
                  value={reportClassId}
                  onChange={(e) => setReportClassId(Number(e.target.value))}
                  className="form-control"
                >
                  <option value={0}>Select class</option>
                  {classes.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
            )}

            {reportType === 'teacher' && (
              <div className="form-group">
                <label htmlFor="report-teacher">Teacher</label>
                <select
                  id="report-teacher"
                  value={reportTeacherId}
                  onChange={(e) => setReportTeacherId(Number(e.target.value))}
                  className="form-control"
                >
                  <option value={0}>All teachers</option>
                  {teachers.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.fullName}
                    </option>
                  ))}
                </select>
              </div>
            )}
          </div>

          <button onClick={handleLoadTimetable} className="btn primary" disabled={isLoading}>
            {isLoading ? 'Loading...' : '🔍 Load Timetable'}
          </button>

          {timetable.length > 0 && (
            <button 
              onClick={() => {
                const table = document.getElementById('main-timetable-table') as HTMLTableElement;
                const title = reportType === 'class' 
                  ? `Timetable - ${classes.find(c => c.id === reportClassId)?.name || 'All Classes'}`
                  : reportTeacherId === 0 
                    ? 'Timetable - All Teachers'
                    : `Timetable - ${teachers.find(t => t.id === reportTeacherId)?.fullName || 'Teacher'}`;
                exportToPDF(table, title);
              }} 
              className="btn secondary"
            >
              📄 Export PDF
            </button>
          )}

          {message && <div className="alert info">{message}</div>}

          {timetable.length > 0 && renderGrid(timetable, sortedPeriods, reportType, 'main-timetable-table')}
          {reportType === 'teacher' && reportTeacherId !== 0 && timetable.length > 0 && (
            <p className="total-badge">{timetable.length} lectures/week</p>
          )}
          {timetable.length > 0 && renderLectureActions(timetable)}
          {!isLoading && timetable.length === 0 && !message && (
            <p className="no-data">No lectures to display for this selection.</p>
          )}
        </div>
      </div>
    );
  };

  const renderManage = () => {
    const items: { id: number; label: string; detail: string; period?: Period; room?: Room }[] = [];
    if (manageType === 'teacher')
      teachers.forEach((item) => items.push({ id: item.id, label: item.fullName, detail: 'Teacher' }));
    if (manageType === 'class')
      classes.forEach((item) => items.push({ id: item.id, label: item.name, detail: 'Class' }));
    if (manageType === 'subject')
      subjects.forEach((item) => items.push({ id: item.id, label: item.name, detail: 'Subject' }));
    if (manageType === 'period')
      periods.forEach((item) => items.push({
        id: item.id,
        label: item.label,
        detail: `${item.startsAt}–${item.endsAt}`,
        period: item,
      }));
    if (manageType === 'infrastructure')
      rooms.forEach((item) => items.push({
        id: item.id,
        label: item.name,
        detail: 'Infrastructure / Room',
        room: item,
      }));

    return (
      <div className="container">
        <div className="card">
          <div className="card-header">
            <h1>Manage Timetable Data</h1>
            <p className="subtitle">Create, update, and remove teachers, classes, subjects, periods, and infrastructure locations.</p>
          </div>
          <div className="form-group">
            <label htmlFor="manage-type">Data type</label>
            <select id="manage-type" value={manageType} onChange={(e) => {
              setManageType(e.target.value as typeof manageType);
              setEditingLookupId(null);
              setManagedName('');
              setNewPeriodLabel('');
              setNewPeriodStart('');
              setNewPeriodEnd('');
              setNewRoomFloor('');
              setNewRoomNumber('');
              setMessage('');
            }} className="form-control">
              <option value="teacher">Teachers</option>
              <option value="class">Classes</option>
              <option value="subject">Subjects</option>
              <option value="period">Periods</option>
              <option value="infrastructure">Infrastructure / Rooms</option>
            </select>
          </div>
          <section className="manage-section">
            <h3>{editingLookupId === null ? `Add ${manageType === 'infrastructure' ? 'infrastructure location' : manageType}` : `Edit ${manageType === 'infrastructure' ? 'infrastructure location' : manageType}`}</h3>
            {manageType === 'infrastructure' ? (
              <div className="form-grid">
                <div className="form-group">
                  <label htmlFor="infrastructure-floor">Floor number</label>
                  <input id="infrastructure-floor" type="number" min="0" max="2147483647" step="1" value={newRoomFloor} onChange={(e) => setNewRoomFloor(e.target.value)} className="form-control" required />
                </div>
                <div className="form-group">
                  <label htmlFor="infrastructure-room">Room number</label>
                  <input id="infrastructure-room" type="number" min="1" max="2147483647" step="1" value={newRoomNumber} onChange={(e) => setNewRoomNumber(e.target.value)} className="form-control" required />
                </div>
              </div>
            ) : manageType !== 'period' ? (
              <div className="form-group">
                <label htmlFor="managed-name">{manageType === 'teacher' ? 'Teacher name' : `${manageType} name`}</label>
                <input
                  id="managed-name"
                  type="text"
                  value={managedName}
                  maxLength={manageType === 'teacher' ? 120 : manageType === 'class' ? 80 : 100}
                  onChange={(e) => setManagedName(e.target.value)}
                  className="form-control"
                  placeholder={manageType === 'teacher' ? 'e.g., Asha Demo' : manageType === 'class' ? 'e.g., 10 A' : 'e.g., Mathematics'}
                  required
                />
              </div>
            ) : (
              <div className="form-grid">
                <div className="form-group">
                  <label htmlFor="period-label">Period label</label>
                  <input id="period-label" type="text" value={newPeriodLabel} maxLength={30} onChange={(e) => setNewPeriodLabel(e.target.value)} className="form-control" placeholder="e.g., P7" required />
                </div>
                <div className="form-group">
                  <label htmlFor="period-start">Start time</label>
                  <input id="period-start" type="time" value={newPeriodStart} onChange={(e) => setNewPeriodStart(e.target.value)} className="form-control" required />
                </div>
                <div className="form-group">
                  <label htmlFor="period-end">End time</label>
                  <input id="period-end" type="time" value={newPeriodEnd} onChange={(e) => setNewPeriodEnd(e.target.value)} className="form-control" required />
                </div>
              </div>
            )}
            <div className="buttons">
              <button onClick={handleSaveManagedItem} className="btn primary" disabled={isAdding}>
                {isAdding ? 'Saving…' : editingLookupId === null ? `Add ${manageType === 'infrastructure' ? 'infrastructure location' : manageType}` : `Save ${manageType === 'infrastructure' ? 'infrastructure location' : manageType}`}
              </button>
              {editingLookupId !== null && (
                <button onClick={() => {
                  setEditingLookupId(null);
                  setManagedName('');
                  setNewPeriodLabel('');
                  setNewPeriodStart('');
                  setNewPeriodEnd('');
                  setNewRoomFloor('');
                  setNewRoomNumber('');
                }} className="btn secondary">Cancel</button>
              )}
            </div>
          </section>
          {message && <div className="alert info" role="status">{message}</div>}
          <div className="data-list">
            <h4>Existing {manageType}s ({items.length})</h4>
            {items.length === 0 ? <p className="no-data">No records have been added.</p> : (
              <ul>
                {items.map((item) => (
                  <li key={item.id} className="managed-item">
                    <span><strong>{item.label}</strong><small>{item.detail}</small></span>
                    <span className="item-actions">
                      <button type="button" className="btn-link" onClick={() => handleEditManagedItem(item.id, item.label, item.period, item.room)}>Edit</button>
                      <button type="button" className="btn-link danger-link" onClick={() => handleDeleteManagedItem(item.id, item.label)} disabled={isAdding}>Delete</button>
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </div>
          <p className="help-note">Records referenced by scheduled lectures cannot be deleted. Update the record or remove its lectures first.</p>
        </div>
      </div>
    );
  };

  const exportToPDF = (tableElement: HTMLTableElement | null, title: string) => {
    if (!tableElement) return;

    const doc = new jsPDF();

    doc.setFontSize(18);
    doc.text(title, 14, 20);
    doc.setFontSize(12);
    doc.text(`Generated on ${new Date().toLocaleDateString()}`, 14, 30);

    // Extract table data manually
    const rows: string[][] = [];
    const headers: string[] = [];

    // Get headers
    const headerRow = tableElement.querySelector('thead tr');
    if (headerRow) {
      const headerCells = headerRow.querySelectorAll('th');
      headerCells.forEach(cell => {
        headers.push(cell.textContent || '');
      });
    }

    // Get body rows
    const bodyRows = tableElement.querySelectorAll('tbody tr');
    bodyRows.forEach(row => {
      const cells = row.querySelectorAll('td');
      const rowData: string[] = [];
      cells.forEach(cell => {
        // Handle nested divs in cells
        const subjectDiv = cell.querySelector('.subject-name');
        const infoDiv = cell.querySelector('.info-text');
        const periodLabel = cell.querySelector('.period-label-text');
        const periodTime = cell.querySelector('.period-time-text');

        if (subjectDiv && infoDiv) {
          const locationDiv = cell.querySelector('.location-text');
          rowData.push(`${subjectDiv.textContent}\n${infoDiv.textContent}${locationDiv ? `\n${locationDiv.textContent}` : ''}`);
        } else if (periodLabel && periodTime) {
          rowData.push(`${periodLabel.textContent}\n${periodTime.textContent}`);
        } else {
          rowData.push(cell.textContent || '');
        }
      });
      rows.push(rowData);
    });

    autoTable(doc, {
      head: [headers],
      body: rows,
      startY: 40,
      styles: {
        fontSize: 9,
        cellPadding: 3,
        halign: 'left',
        valign: 'middle',
      },
      headStyles: {
        fillColor: [79, 70, 229],
        textColor: 255,
        fontStyle: 'bold',
        halign: 'center',
      },
      columnStyles: {
        0: { cellWidth: 30, fontStyle: 'bold' }, // Period column
      },
      theme: 'grid',
    });

    doc.save('timetable.pdf');
  };

  const renderGrid = (lectures: Lecture[], sortedPeriods: Period[], viewType: 'class' | 'teacher', tableId?: string) => {
    return (
      <div className="timetable-grid">
        <table id={tableId}>
          <thead>
            <tr>
              <th>Period</th>
              {DAYS.map((day) => (
                <th key={day.value}>{day.short}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {sortedPeriods.map((period) => (
              <tr key={period.id}>
                <td className="period-label">
                  <div className="period-time">
                    <span className="period-label-text">{period.label}</span>
                    <span className="period-time-text">{period.startsAt}–{period.endsAt}</span>
                  </div>
                </td>
                {DAYS.map((day) => {
                  const lecture = lectures.find(
                    (l) => l.dayOfWeek === day.value && l.periodId === period.id
                  );
                  return (
                    <td key={day.value}>
                      {lecture && (
                        <div className="cell-content">
                          <div className="subject-name">{lecture.subjectName}</div>
                          <div className="info-text">
                            {viewType === 'class' ? lecture.teacherName : lecture.className}
                          </div>
                          <div className="location-text">{lecture.roomName || 'Location not assigned'}</div>
                        </div>
                      )}
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  };

  if (universityLoading) {
    return <main className="university-gate"><div className="university-gate-card"><div className="university-brand-mark">U</div><p className="eyebrow">CAMPUSDESK · UNIVERSITY WORKSPACE</p><h1>Preparing your workspace</h1><p>Loading the university directory…</p><div className="university-loading-bar" /></div></main>;
  }

  if (!activeUniversity) {
    return (
      <main className="university-gate">
        <section className="university-gate-card">
          <div className="university-brand-mark">U</div>
          <p className="eyebrow">CAMPUSDESK · UNIVERSITY WORKSPACE</p>
          <h1>{showUniversityRegistration ? 'Register your university' : 'Welcome to your campus'}</h1>
          <p className="university-intro">Choose your university workspace or create one to organize its timetable, classes, teachers, and infrastructure under one institution.</p>
          {universityError && <div className="alert error" role="alert">{universityError}</div>}
          {showUniversityRegistration ? (
            <form className="university-registration" onSubmit={registerUniversity}>
              <label>University name<input required minLength={2} maxLength={160} value={universityForm.name} onChange={(e) => setUniversityForm({ ...universityForm, name: e.target.value })} placeholder="e.g. North Valley University" /></label>
              <div className="university-form-grid">
                <label>Country<input required maxLength={100} value={universityForm.country} onChange={(e) => setUniversityForm({ ...universityForm, country: e.target.value })} placeholder="Country" /></label>
                <label>State / province<input required maxLength={100} value={universityForm.state} onChange={(e) => setUniversityForm({ ...universityForm, state: e.target.value })} placeholder="State" /></label>
                <label>District<input required maxLength={100} value={universityForm.district} onChange={(e) => setUniversityForm({ ...universityForm, district: e.target.value })} placeholder="District" /></label>
                <label>City<input required maxLength={100} value={universityForm.city} onChange={(e) => setUniversityForm({ ...universityForm, city: e.target.value })} placeholder="City" /></label>
              </div>
              <label>University contact email<input required type="email" maxLength={254} value={universityForm.contactEmail} onChange={(e) => setUniversityForm({ ...universityForm, contactEmail: e.target.value })} placeholder="admin@university.edu" /></label>
              <div className="university-gate-actions">
                <button className="btn primary" type="submit" disabled={universitySaving}>{universitySaving ? 'Creating workspace…' : 'Create university workspace'}</button>
                <button className="btn secondary" type="button" onClick={() => { setShowUniversityRegistration(false); setUniversityError(''); }}>Back to university list</button>
              </div>
            </form>
          ) : (
            <>
              <label className="university-select-label" htmlFor="university-choice">University</label>
              <select id="university-choice" className="form-control" value={selectedUniversityId} onChange={(e) => setSelectedUniversityId(e.target.value)}>
                <option value="">Select a university</option>
                {universities.map((university) => <option key={university.id} value={university.id}>{university.name} · {university.city}</option>)}
              </select>
              <div className="university-gate-actions">
                <button className="btn primary" disabled={!selectedUniversityId} onClick={() => {
                  const selected = universities.find((university) => university.id === Number(selectedUniversityId));
                  if (selected) void enterUniversity(selected);
                }}>Continue to workspace</button>
                <button className="btn secondary" onClick={() => { setShowUniversityRegistration(true); setUniversityError(''); }}>+ Add a university</button>
              </div>
              <p className="university-footnote">Your university has its own rooms, class schedule, subjects, and staff records.</p>
            </>
          )}
        </section>
      </main>
    );
  }

  return (
    <div className="app">
      <nav className="nav">
        <button
          onClick={() => setView('form')}
          className={view === 'form' ? 'active' : ''}
        >
          📝 Lecture Form
        </button>
        <button
          onClick={() => setView('report')}
          className={view === 'report' ? 'active' : ''}
        >
          📊 Timetable Report
        </button>
        <button
          onClick={() => setView('manage')}
          className={view === 'manage' ? 'active' : ''}
        >
          ⚙️ Manage Data
        </button>
        <div className="university-context">
          <span className="university-context-label">Workspace</span>
          <strong>{activeUniversity.name}</strong>
          <button type="button" onClick={switchUniversity}>Switch university</button>
        </div>
      </nav>
      {view === 'form' ? renderForm() : view === 'report' ? renderReport() : renderManage()}
    </div>
  );
}

export default App;
