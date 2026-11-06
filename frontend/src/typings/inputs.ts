import { ReactNode } from 'react';

export type InputChanged<T = unknown> = {
  name: string;
  value: T;
};

export type CheckInputChanged = InputChanged<boolean>;

export interface EnhancedSelectInputValue<V> {
  key: V;
  value: string;
  hint?: ReactNode;
  isDisabled?: boolean;
  isHidden?: boolean;
  parentKey?: V;
  additionalProperties?: object;
}
