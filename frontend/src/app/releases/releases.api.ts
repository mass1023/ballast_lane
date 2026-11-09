import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type ReleaseFormat = 'Single' | 'EP' | 'Album';

export const releaseFormats: ReleaseFormat[] = ['Single', 'EP', 'Album'];

export interface Release {
  id: string;
  title: string;
  artist: string;
  releaseYear: number;
  format: ReleaseFormat;
  createdAt: string;
  updatedAt: string;
}

export interface SaveRelease {
  title: string;
  artist: string;
  releaseYear: number;
  format: ReleaseFormat;
}

export interface Page<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ReleaseQuery {
  search: string;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class ReleasesApi {
  private httpClient = inject(HttpClient);
  private endpoint = `${environment.apiUrl}/releases`;

  search(query: ReleaseQuery): Observable<Page<Release>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.search) {
      params = params.set('search', query.search);
    }
    return this.httpClient.get<Page<Release>>(this.endpoint, { params });
  }

  get(id: string): Observable<Release> {
    return this.httpClient.get<Release>(`${this.endpoint}/${id}`);
  }

  create(release: SaveRelease): Observable<Release> {
    return this.httpClient.post<Release>(this.endpoint, release);
  }

  update(id: string, release: SaveRelease): Observable<Release> {
    return this.httpClient.put<Release>(`${this.endpoint}/${id}`, release);
  }

  delete(id: string): Observable<void> {
    return this.httpClient.delete<void>(`${this.endpoint}/${id}`);
  }
}
