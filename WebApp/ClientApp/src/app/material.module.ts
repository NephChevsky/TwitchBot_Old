import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';

const modules = [CommonModule, MatTableModule, MatSortModule];

@NgModule({
	imports: [...modules],
	exports: [...modules],
	providers: []
})

export class MaterialModule { }
