declare module "cs2/api" {
  export interface ValueBinding<T> {
    readonly value: T;
    subscribe(listener?: (value: T) => void): { dispose(): void };
    dispose(): void;
  }
  export function bindValue<T>(group: string, name: string, fallbackValue?: T): ValueBinding<T>;
  export function useValue<T>(binding: ValueBinding<T>): T;
  export function trigger(group: string, name: string, ...args: unknown[]): void;
}

declare module "cs2/modding" {
  import type { ComponentType } from "react";
  export type ModuleRegistryAppend = ComponentType<Record<string, never>> | (() => JSX.Element);
  export type AppendHookTargets = "Menu" | "Editor" | "Game" | "GameTopLeft" | "GameTopRight" | "GameBottomRight";
  export interface ModuleRegistry {
    append(target: AppendHookTargets, component: ModuleRegistryAppend, index?: number): void;
  }
  export type ModRegistrar = (moduleRegistry: ModuleRegistry) => void;
}

declare module "cs2/ui" {
  import type { ButtonHTMLAttributes, PropsWithChildren, ReactElement, ReactNode } from "react";

  export interface ButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, "onSelect"> {
    variant?: "flat" | "primary" | "round" | "menu" | "icon" | "floating" | "default";
    selected?: boolean;
    src?: string;
    tooltipLabel?: ReactNode;
    onSelect?: () => void;
    as?: "button" | "div";
  }

  export const Button: (props: PropsWithChildren<ButtonProps>) => JSX.Element;

  export interface TooltipProps {
    tooltip: ReactNode;
    disabled?: boolean;
    children: ReactElement;
  }

  export const Tooltip: (props: TooltipProps) => JSX.Element;
}
