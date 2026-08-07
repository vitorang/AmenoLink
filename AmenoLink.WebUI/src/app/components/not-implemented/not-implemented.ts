import { Component } from '@angular/core';
import { EmptyState } from '../empty-state/empty-state';

@Component({
    selector: 'app-not-implemented',
    imports: [EmptyState],
    templateUrl: './not-implemented.html',
    styleUrl: './not-implemented.scss',
})
export class NotImplemented {}
