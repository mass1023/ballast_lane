import { Component, inject, input, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { serverErrors } from '../core/api-errors';
import { fieldError } from '../core/field-error';
import { ErrorBox } from '../shared/error-box';
import { ReleaseFormat, releaseFormats, ReleasesApi, SaveRelease } from './releases.api';

export const minYear = 1900;
export const maxTextLength = 200;

@Component({
  selector: 'app-release-form',
  imports: [ReactiveFormsModule, RouterLink, ErrorBox],
  templateUrl: './release-form.html',
})
export class ReleaseForm implements OnInit {
  private releases = inject(ReleasesApi);
  private router = inject(Router);

  readonly id = input<string>();

  protected readonly formats = releaseFormats;
  protected readonly minYear = minYear;
  protected readonly maxYear = new Date().getFullYear() + 1;
  protected readonly fieldError = fieldError;

  protected readonly form = new FormGroup({
    title: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(maxTextLength)],
    }),
    artist: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(maxTextLength)],
    }),
    releaseYear: new FormControl<number | null>(null, [
      Validators.required,
      Validators.min(minYear),
      Validators.max(this.maxYear),
    ]),
    format: new FormControl<ReleaseFormat>('Album', { nonNullable: true, validators: Validators.required }),
  });

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly loadFailed = signal(false);

  ngOnInit(): void {
    const id = this.id();
    if (!id) return;

    this.loading.set(true);
    this.releases.get(id).subscribe({
      next: (release) => {
        this.form.setValue({
          title: release.title,
          artist: release.artist,
          releaseYear: release.releaseYear,
          format: release.format,
        });
        this.loading.set(false);
      },
      error: (error) => {
        this.errors.set(serverErrors(error));
        this.loadFailed.set(true);
        this.loading.set(false);
      },
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const release: SaveRelease = {
      title: value.title.trim(),
      artist: value.artist.trim(),
      releaseYear: value.releaseYear!,
      format: value.format,
    };

    const id = this.id();
    const request = id ? this.releases.update(id, release) : this.releases.create(release);

    this.saving.set(true);
    this.errors.set([]);
    request.subscribe({
      next: () => this.router.navigate(['/releases']),
      error: (error) => {
        this.saving.set(false);
        this.errors.set(serverErrors(error));
      },
    });
  }
}
