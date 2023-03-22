import { findLast } from 'lodash-es';
import isSameCommand from './isSameCommand';

function findCommand(commands, options) {
  return findLast(commands, (command) => {
    return isSameCommand(command.body, options);
  });
}

export default findCommand;
