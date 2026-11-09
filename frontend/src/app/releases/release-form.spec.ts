import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ReleaseForm } from './release-form';
import { Release, ReleasesApi } from './releases.api';

describe('ReleaseForm', () => {
  let fixture: ComponentFixture<ReleaseForm>;
  let element: HTMLElement;
  let api: { get: ReturnType<typeof vi.fn>; create: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };

  const release: Release = {
    id: 'r1',
    title: 'Kind of Blue',
    artist: 'Miles Davis',
    releaseYear: 1959,
    format: 'Album',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };

  async function render(id?: string) {
    fixture = TestBed.createComponent(ReleaseForm);
    if (id) fixture.componentRef.setInput('id', id);
    await fixture.whenStable();
    element = fixture.nativeElement;
  }

  function type(selector: string, value: string) {
    const input = element.querySelector<HTMLInputElement | HTMLSelectElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event(input instanceof HTMLSelectElement ? 'change' : 'input'));
    input.dispatchEvent(new Event('blur'));
  }

  async function submit() {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  beforeEach(() => {
    api = { get: vi.fn(() => of(release)), create: vi.fn(() => of(release)), update: vi.fn(() => of(release)) };
    TestBed.configureTestingModule({
      imports: [ReleaseForm],
      providers: [provideRouter([]), { provide: ReleasesApi, useValue: api }],
    });
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  it('shows every required-field error on an empty submit and does not call the API', async () => {
    await render();

    await submit();

    expect(element.textContent).toContain('Title is required.');
    expect(element.textContent).toContain('Artist is required.');
    expect(element.textContent).toContain('Release year is required.');
    expect(api.create).not.toHaveBeenCalled();
  });

  it('rejects a year outside 1900 to next year', async () => {
    await render();
    const nextYear = new Date().getFullYear() + 1;

    type('#releaseYear', '1899');
    await fixture.whenStable();
    expect(element.textContent).toContain('Release year must be 1900 or later.');

    type('#releaseYear', String(nextYear + 1));
    await fixture.whenStable();
    expect(element.textContent).toContain(`Release year must be ${nextYear} or earlier.`);
  });

  it('creates a trimmed release and goes back to the list', async () => {
    await render();
    type('#title', '  Blue Train ');
    type('#artist', 'John Coltrane');
    type('#releaseYear', '1957');
    type('#format', 'EP');

    await submit();

    expect(api.create).toHaveBeenCalledWith({
      title: 'Blue Train',
      artist: 'John Coltrane',
      releaseYear: 1957,
      format: 'EP',
    });
    expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith(['/releases']);
  });

  it('loads the release when editing and saves with update', async () => {
    await render('r1');

    expect(api.get).toHaveBeenCalledWith('r1');
    expect(element.querySelector<HTMLInputElement>('#title')!.value).toBe('Kind of Blue');

    await submit();

    expect(api.update).toHaveBeenCalledWith('r1', {
      title: 'Kind of Blue',
      artist: 'Miles Davis',
      releaseYear: 1959,
      format: 'Album',
    });
  });

  it('lists the API errors in the error box', async () => {
    api.create.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { title: 'One or more validation errors occurred.', errors: { title: ['Title from the server.'], artist: ['Artist from the server.'] } },
          }),
      ),
    );
    await render();
    type('#title', 'x');
    type('#artist', 'y');
    type('#releaseYear', '2000');

    await submit();

    const box = element.querySelector('app-error-box')!;
    expect(box.textContent).toContain('Title from the server.');
    expect(box.textContent).toContain('Artist from the server.');
  });

  it('shows a non-field API error, like a missing release, in the same box', async () => {
    api.update.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 404, error: { title: 'Release r1 was not found.' } })),
    );
    await render('r1');

    await submit();

    expect(element.querySelector('app-error-box')!.textContent).toContain('Release r1 was not found.');
  });
});
